using Microsoft.AspNetCore.Authorization;
using Thrivts.Application.Common.Security;
using Thrivts.Domain.Identity;

namespace Thrivts.Infrastructure.Identity;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>Resolves the caller's role from the token and checks it against the single
/// role → permission map in <see cref="Permissions"/>.</summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(JwtOptions.RoleClaim)?.Value;
        if (Enum.TryParse<UserRole>(roleClaim, out var role) && Permissions.Has(role, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
