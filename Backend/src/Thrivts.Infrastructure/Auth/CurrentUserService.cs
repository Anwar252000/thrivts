using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Infrastructure.Auth;

/// <summary>
/// Reads the caller straight off the validated Supabase JWT already sitting on HttpContext.User —
/// no per-handler DB call. The role claim is populated once per request by
/// <see cref="ProfileRoleClaimsTransformation"/>, right after authentication.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            // Supabase issues the auth.uid() as the standard JWT `sub` claim.
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var role = User?.FindFirstValue(ProfileRoleClaimsTransformation.RoleClaimType);
            return Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) ? parsed : null;
        }
    }
}
