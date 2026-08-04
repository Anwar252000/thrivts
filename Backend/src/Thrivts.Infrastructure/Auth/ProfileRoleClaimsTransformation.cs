using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Thrivts.Infrastructure.Persistence;

namespace Thrivts.Infrastructure.Auth;

/// <summary>
/// Runs once per authenticated request (ASP.NET Core caches the result for the request's
/// lifetime). Looks up profiles.role for the token's `sub` and stamps it as a claim, so
/// [Authorize(Policy = "AdminOnly")] etc. and ICurrentUserService.Role never re-query the DB.
/// </summary>
public class ProfileRoleClaimsTransformation : IClaimsTransformation
{
    public const string RoleClaimType = "thrivts:role";

    private readonly ThrivtsDbContext _db;

    public ProfileRoleClaimsTransformation(ThrivtsDbContext db)
    {
        _db = db;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == RoleClaimType))
            return principal;

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
            return principal;

        var profile = await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId);

        if (profile is null || !profile.IsActive)
            return principal;

        var identity = (ClaimsIdentity)principal.Identity;
        identity.AddClaim(new Claim(RoleClaimType, profile.Role.ToString()));

        return principal;
    }
}
