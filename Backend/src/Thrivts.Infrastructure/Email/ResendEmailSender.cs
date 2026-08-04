using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Email;

/// <summary>
/// Sends via the Resend REST API (the same provider already used for Supabase Auth emails and
/// the old thrivts-notify Edge Function). Registered as a typed HttpClient with the standard
/// resilience handler (retry + circuit breaker) — see DependencyInjection.cs.
/// </summary>
public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;

    public ResendEmailSender(HttpClient httpClient, IOptions<ResendOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress ??= new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            from = _options.FromAddress,
            to = new[] { toEmail },
            subject,
            html = htmlBody
        };

        var response = await _httpClient.PostAsJsonAsync("emails", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
