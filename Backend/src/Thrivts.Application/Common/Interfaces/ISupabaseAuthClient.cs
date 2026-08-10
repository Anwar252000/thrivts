namespace Thrivts.Application.Common.Interfaces;

/// <summary>
/// The .NET API's only direct contact with Supabase Auth (GoTrue's REST API). The browser never
/// talks to Supabase at all — every auth call is proxied through this API, so the API is the
/// single client of Supabase end to end, for auth as well as data (see migration plan §2's
/// "Key shift" and README_HANDOVER.md).
/// </summary>
public interface ISupabaseAuthClient
{
    Task<SupabaseSession> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<SupabaseSession> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task SignOutAsync(string accessToken, CancellationToken cancellationToken = default);
}

public record SupabaseSession(string AccessToken, string RefreshToken, int ExpiresIn, Guid UserId, string Email);
