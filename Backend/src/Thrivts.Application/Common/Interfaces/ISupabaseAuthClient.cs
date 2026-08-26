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

    /// <summary>Sends a password-recovery email via GoTrue. Always succeeds regardless of whether
    /// the email is registered — GoTrue never reveals account existence through this endpoint.</summary>
    Task RequestPasswordResetAsync(string email, string redirectTo, CancellationToken cancellationToken = default);

    /// <summary>Sets a new password for the user identified by a recovery access token — the token
    /// from the link GoTrue emailed, not a normal login session.</summary>
    Task ResetPasswordAsync(string recoveryAccessToken, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Self-service signup via the anon key (not the admin API) — GoTrue creates an
    /// unconfirmed auth identity, stashes <paramref name="metadata"/> as raw_user_meta_data, and
    /// emails a confirmation link to redirectTo. No Profile/role row is created yet — that only
    /// happens once the link is clicked, via CompleteBuyerRegistrationCommand reading this
    /// metadata back through ISupabaseAdminClient.GetUserAsync.</summary>
    Task SignUpAsync(string email, string password, IDictionary<string, object?> metadata, string redirectTo, CancellationToken cancellationToken = default);
}

public record SupabaseSession(string AccessToken, string RefreshToken, int ExpiresIn, Guid UserId, string Email);
