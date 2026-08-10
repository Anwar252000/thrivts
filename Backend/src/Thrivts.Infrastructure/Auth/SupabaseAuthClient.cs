using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Auth;

/// <summary>
/// Talks to Supabase Auth's REST API (GoTrue) — the same endpoints the Supabase JS client would
/// have called from the browser, now called from here instead. Registered as a typed HttpClient
/// with the standard resilience handler — see DependencyInjection.cs.
/// </summary>
public class SupabaseAuthClient : ISupabaseAuthClient
{
    private readonly HttpClient _httpClient;

    public SupabaseAuthClient(HttpClient httpClient, IOptions<SupabaseAuthOptions> options)
    {
        _httpClient = httpClient;
        var supabaseOptions = options.Value;
        _httpClient.BaseAddress ??= new Uri($"{supabaseOptions.Url.TrimEnd('/')}/auth/v1/");
        _httpClient.DefaultRequestHeaders.Add("apikey", supabaseOptions.AnonKey);
    }

    public async Task<SupabaseSession> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "token?grant_type=password",
            new { email, password },
            cancellationToken);

        return await ParseSessionAsync(response, cancellationToken);
    }

    public async Task<SupabaseSession> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "token?grant_type=refresh_token",
            new { refresh_token = refreshToken },
            cancellationToken);

        return await ParseSessionAsync(response, cancellationToken);
    }

    public async Task SignOutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        // An already-expired/invalid session is not worth failing the caller's logout over.
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
            response.EnsureSuccessStatusCode();
    }

    private static async Task<SupabaseSession> ParseSessionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken);
            throw new SupabaseAuthException(error?.ErrorDescription ?? error?.Msg ?? "Authentication failed.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken)
            ?? throw new SupabaseAuthException("Supabase returned an empty response.");

        return new SupabaseSession(
            payload.AccessToken,
            payload.RefreshToken,
            payload.ExpiresIn,
            payload.User.Id,
            payload.User.Email);
    }

    private sealed record SupabaseTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("user")] SupabaseUser User);

    private sealed record SupabaseUser(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string Email);

    private sealed record SupabaseErrorResponse(
        [property: JsonPropertyName("error_description")] string? ErrorDescription,
        [property: JsonPropertyName("msg")] string? Msg);
}
