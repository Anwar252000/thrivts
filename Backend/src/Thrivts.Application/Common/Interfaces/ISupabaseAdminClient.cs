namespace Thrivts.Application.Common.Interfaces;

/// <summary>
/// Supabase Auth's ADMIN API (service_role key, not the anon key ISupabaseAuthClient uses) — the
/// only way to create or remove a login identity directly, bypassing self-service signup. Kept as
/// a separate interface/client from ISupabaseAuthClient specifically so the far more powerful
/// service_role key (bypasses RLS entirely) is never reachable from the ordinary login/refresh/
/// logout/password-reset code paths, only from admin-user-management commands.
/// </summary>
public interface ISupabaseAdminClient
{
    /// <summary>Creates a Supabase Auth user directly (admin-vouched, so email_confirm is set —
    /// no verification email). Returns the new user's id, which becomes the Profile's id.</summary>
    Task<Guid> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Removes a Supabase Auth identity outright. Only used to compensate a
    /// CreateUserAsync whose follow-up Profile/role-row write failed — see CreateUserCommand.</summary>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Reads back an auth identity's email and raw_user_meta_data — used by
    /// CompleteBuyerRegistrationCommand to recover the signup form's fields once the user has
    /// confirmed their email (SignUpAsync stashed them as metadata; nothing was written to our own
    /// DB before confirmation).</summary>
    Task<SupabaseAdminUser> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record SupabaseAdminUser(Guid Id, string Email, System.Text.Json.JsonElement Metadata, bool EmailConfirmed);
