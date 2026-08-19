namespace Thrivts.Infrastructure.Auth;

public class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = default!;
    public string AnonKey { get; set; } = default!;

    /// <summary>Bypasses RLS entirely — only ever used by ISupabaseAdminClient (admin user
    /// creation), never by the ordinary auth flows that use AnonKey.</summary>
    public string? ServiceRoleKey { get; set; }
}
