using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Auth;

/// <summary>Talks to Supabase Auth's admin REST API using the service_role key — see
/// ISupabaseAdminClient for why this is a separate client from SupabaseAuthClient.</summary>
public class SupabaseAdminClient : ISupabaseAdminClient
{
    private readonly HttpClient _httpClient;

    public SupabaseAdminClient(HttpClient httpClient, IOptions<SupabaseAuthOptions> options)
    {
        _httpClient = httpClient;
        var supabaseOptions = options.Value;
        var serviceRoleKey = supabaseOptions.ServiceRoleKey
            ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is not configured.");

        _httpClient.BaseAddress ??= new Uri($"{supabaseOptions.Url.TrimEnd('/')}/auth/v1/");
        _httpClient.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
    }

    public async Task<Guid> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "admin/users",
            new { email, password, email_confirm = true },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken);
            throw new SupabaseAuthException(error?.Msg ?? error?.ErrorDescription ?? "Could not create the user.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SupabaseAdminUserResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken)
            ?? throw new SupabaseAuthException("Supabase returned an empty response.");

        return payload.Id;
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"admin/users/{userId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken);
            throw new SupabaseAuthException(error?.Msg ?? error?.ErrorDescription ?? "Could not delete the user.");
        }
    }

    public async Task<SupabaseAdminUser> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"admin/users/{userId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken);
            throw new SupabaseAuthException(error?.Msg ?? error?.ErrorDescription ?? "Could not read the user.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SupabaseAdminUserDetailResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken)
            ?? throw new SupabaseAuthException("Supabase returned an empty response.");

        return new SupabaseAdminUser(payload.Id, payload.Email, payload.UserMetadata, payload.EmailConfirmedAt is not null);
    }

    private sealed record SupabaseAdminUserResponse([property: JsonPropertyName("id")] Guid Id);

    private sealed record SupabaseAdminUserDetailResponse(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("user_metadata")] System.Text.Json.JsonElement UserMetadata,
        [property: JsonPropertyName("email_confirmed_at")] DateTimeOffset? EmailConfirmedAt);

    private sealed record SupabaseErrorResponse(
        [property: JsonPropertyName("msg")] string? Msg,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);
}
