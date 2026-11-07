using System.Text.RegularExpressions;
using Serilog.Context;
using Thrivts.Infrastructure.Identity;

namespace Thrivts.Api.Middleware;

/// <summary>
/// Gives every request a correlation id (taken from X-Correlation-Id if the app sent a sane one,
/// otherwise generated), echoes it back, and attaches it plus the user id to every log line written
/// while handling the request — so one id finds everything that happened for one tap in the app.
/// </summary>
public sealed partial class RequestContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.Items[CurrentUser.CorrelationItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex SafeId();
}

/// <summary>Defensive headers for a JSON-only API: nothing may frame it, sniff types, leak
/// referrers, or load content from it.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Resource-Policy"] = "cross-origin";

        if (!context.Request.Path.StartsWithSegments("/scalar") && !context.Request.Path.StartsWithSegments("/openapi"))
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        return next(context);
    }
}
