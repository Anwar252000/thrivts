using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Infrastructure.Auth;

namespace Thrivts.Infrastructure.Storage;

/// <summary>Talks to Supabase Storage's REST API using the service_role key — see
/// ISupabaseStorageClient for why this is server-side-only.</summary>
public class SupabaseStorageClient : ISupabaseStorageClient
{
    private readonly HttpClient _httpClient;
    private readonly string _publicBaseUrl;

    public SupabaseStorageClient(HttpClient httpClient, IOptions<SupabaseAuthOptions> options)
    {
        _httpClient = httpClient;
        var supabaseOptions = options.Value;
        var serviceRoleKey = supabaseOptions.ServiceRoleKey
            ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is not configured.");

        _publicBaseUrl = $"{supabaseOptions.Url.TrimEnd('/')}/storage/v1";
        _httpClient.BaseAddress ??= new Uri($"{_publicBaseUrl}/");
        _httpClient.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
    }

    public async Task<string> UploadAsync(string bucket, string path, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await _httpClient.PostAsync($"object/{bucket}/{path}", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseStorageErrorResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken);
            throw new SupabaseAuthException(error?.Message ?? "Could not upload the file.");
        }

        return path;
    }

    public async Task<string> CreateSignedUrlAsync(string bucket, string path, int expiresInSeconds, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"object/sign/{bucket}/{path}", new { expiresIn = expiresInSeconds }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<SupabaseStorageErrorResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken);
            throw new SupabaseAuthException(error?.Message ?? "Could not create a signed URL.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SignedUrlResponse>((System.Text.Json.JsonSerializerOptions?)null, cancellationToken)
            ?? throw new SupabaseAuthException("Supabase returned an empty response.");

        // signedURL comes back as a path like "/object/sign/receipts/...?token=..." — make it absolute.
        return payload.SignedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? payload.SignedUrl
            : $"{_publicBaseUrl}{payload.SignedUrl}";
    }

    private sealed record SignedUrlResponse([property: JsonPropertyName("signedURL")] string SignedUrl);

    private sealed record SupabaseStorageErrorResponse(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("error")] string? Error);
}
