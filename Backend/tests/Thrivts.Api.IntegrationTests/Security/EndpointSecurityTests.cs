using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Thrivts.Api.Controllers.V1;
using Thrivts.Application.Common.Security;
using Thrivts.Domain.Identity;

namespace Thrivts.Api.IntegrationTests.Security;

/// <summary>
/// Static guards over the whole API surface — no database needed. They fail the build when a new
/// endpoint forgets its permission, when an /owner route loses its OwnerAccess gate, or when a staff
/// role is ever handed an owner-only permission (the price-confidentiality guarantee).
/// </summary>
public class EndpointSecurityTests
{
    /// <summary>Endpoints that are deliberately only "authenticated" (any signed-in user) or that
    /// authorize imperatively inside the action.</summary>
    private static readonly HashSet<string> AuthenticatedOnly =
    [
        "AuthController.Me",
        "AuthController.ChangePin",
        "OperationsController.Submit",
    ];

    private static readonly UserRole[] StaffRoles = [UserRole.GeneralManager, UserRole.Supervisor, UserRole.InventoryManager, UserRole.GateGuard];

    private static IEnumerable<(Type Controller, MethodInfo Action)> Actions() =>
        typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(ApiControllerBase)))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(m => (t, m)));

    [Fact]
    public void Every_endpoint_declares_a_permission_policy()
    {
        var unguarded = Actions()
            .Where(a => a.Action.GetCustomAttribute<AllowAnonymousAttribute>() is null && a.Controller.GetCustomAttribute<AllowAnonymousAttribute>() is null)
            .Where(a => !AuthenticatedOnly.Contains($"{a.Controller.Name}.{a.Action.Name}"))
            .Where(a => !a.Action.GetCustomAttributes<AuthorizeAttribute>().Concat(a.Controller.GetCustomAttributes<AuthorizeAttribute>())
                .Any(attr => !string.IsNullOrEmpty(attr.Policy)))
            .Select(a => $"{a.Controller.Name}.{a.Action.Name}")
            .ToList();

        unguarded.Should().BeEmpty("every endpoint must name the permission it needs");
    }

    [Fact]
    public void Every_policy_used_by_an_endpoint_is_registered()
    {
        var known = Permissions.All.ToHashSet();
        var unknown = Actions()
            .SelectMany(a => a.Action.GetCustomAttributes<AuthorizeAttribute>().Concat(a.Controller.GetCustomAttributes<AuthorizeAttribute>()))
            .Select(a => a.Policy)
            .Where(p => !string.IsNullOrEmpty(p) && !known.Contains(p))
            .Distinct()
            .ToList();

        unknown.Should().BeEmpty("a policy no role holds would make the endpoint unreachable");
    }

    [Fact]
    public void Owner_routes_are_gated_by_owner_access_at_class_level()
    {
        var owner = typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ApiControllerBase)))
            .Where(t => t.GetCustomAttribute<RouteAttribute>()?.Template.Contains("/owner", StringComparison.Ordinal) == true)
            .ToList();

        owner.Should().NotBeEmpty();
        owner.Should().AllSatisfy(t =>
            t.GetCustomAttributes<AuthorizeAttribute>().Select(a => a.Policy).Should().Contain(Permissions.OwnerAccess, t.Name));
    }

    [Fact]
    public void No_owner_route_allows_anonymous_access()
    {
        Actions()
            .Where(a => a.Controller.GetCustomAttribute<RouteAttribute>()?.Template.Contains("/owner", StringComparison.Ordinal) == true)
            .Should().AllSatisfy(a => a.Action.GetCustomAttribute<AllowAnonymousAttribute>().Should().BeNull(a.Action.Name));
    }

    [Theory]
    [MemberData(nameof(StaffRoleData))]
    public void Staff_roles_never_hold_owner_only_permissions(UserRole role)
    {
        Permissions.Has(role, Permissions.OwnerAccess).Should().BeFalse();
        Permissions.Has(role, Permissions.UsersManage).Should().BeFalse();
    }

    [Fact]
    public void Only_the_owner_manages_users()
    {
        Enum.GetValues<UserRole>().Where(r => Permissions.Has(r, Permissions.UsersManage))
            .Should().Equal(UserRole.Owner);
    }

    [Fact]
    public void Anonymous_endpoints_exist_only_for_login_and_client_portals()
    {
        var anonymous = Actions()
            .Where(a => a.Action.GetCustomAttribute<AllowAnonymousAttribute>() is not null || a.Controller.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(a => a.Controller.Name)
            .Distinct();

        anonymous.Should().BeSubsetOf(["AuthController", "PortalController"]);
    }

    [Fact]
    public void Portal_responses_carry_no_money_or_credentials()
    {
        var forbidden = new[] { "price", "value", "paid", "payment", "amount", "logincode", "code", "total" };
        var portalTypes = typeof(Thrivts.Application.Features.Portal.PortalOrderDto).Assembly.GetTypes()
            .Where(t => t.Namespace == "Thrivts.Application.Features.Portal" && t.Name.StartsWith("Portal", StringComparison.Ordinal) && t.Name.EndsWith("Dto", StringComparison.Ordinal));

        portalTypes.Should().NotBeEmpty();
        portalTypes.SelectMany(t => t.GetProperties().Select(p => $"{t.Name}.{p.Name}"))
            .Where(name => forbidden.Any(f => name.Split('.')[1].Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Should().BeEmpty("a buyer's portal must never see order value, payments or codes");
    }

    public static TheoryData<UserRole> StaffRoleData() => [.. StaffRoles];
}
