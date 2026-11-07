using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Identity;

namespace Thrivts.Infrastructure.Identity;

/// <summary>Reads the caller from the validated JWT on the current request. Singleton-safe: it
/// holds no state of its own, only the accessor.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string CorrelationItemKey = "CorrelationId";

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public string? Name => Principal?.FindFirstValue(JwtRegisteredClaimNames.Name);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(JwtOptions.RoleClaim), out var role) ? role : null;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId => accessor.HttpContext?.Items[CorrelationItemKey] as string ?? accessor.HttpContext?.TraceIdentifier;

    public Guid RequireUserId() => UserId ?? throw new UnauthorizedAccessException("Not signed in.");
}
