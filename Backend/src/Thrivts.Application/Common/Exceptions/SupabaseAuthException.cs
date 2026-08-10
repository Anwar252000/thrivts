namespace Thrivts.Application.Common.Exceptions;

/// <summary>Thrown by ISupabaseAuthClient when Supabase Auth rejects a request (bad credentials, expired/invalid refresh token, etc.).</summary>
public class SupabaseAuthException : Exception
{
    public SupabaseAuthException(string message) : base(message)
    {
    }
}
