namespace Thrivts.Infrastructure.Auth;

public class SupabaseAuthOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = default!;
    public string AnonKey { get; set; } = default!;
}
