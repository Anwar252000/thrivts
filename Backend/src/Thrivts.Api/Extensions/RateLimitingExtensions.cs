using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Thrivts.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string LoginPolicy = "login";
    public const string AssistantPolicy = "assistant";
    public const string PortalPolicy = "portal";

    /// <summary>
    /// Two layers. The PIN login gets a strict per-IP window (10 attempts/minute) on top of the
    /// per-account lockout, so neither guessing one PIN nor spraying many accounts is practical.
    /// Everything else gets a generous per-user token bucket (per-IP when anonymous) that absorbs
    /// normal bursts but stops a runaway client from starving everyone else.
    /// </summary>
    public static IServiceCollection AddThrivtsRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsync(
                    """{"title":"Too many requests — please wait a moment and try again.","status":429}""", token);
            };

            options.AddPolicy(LoginPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            // Every assistant question is a paid model call: cap it per user, on top of the general
            // bucket, so a stuck client or a held-down Enter key can't run up the bill.
            options.AddPolicy(AssistantPolicy, context => RateLimitPartition.GetSlidingWindowLimiter(
                context.User.FindFirst("sub")?.Value ?? $"ip:{context.Connection.RemoteIpAddress}",
                _ => new SlidingWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6, QueueLimit = 0 }));

            // Client portals are anonymous and the order code is the only credential: 20 tries a
            // minute per IP is plenty for a buyer refreshing their page, useless for guessing codes.
            options.AddPolicy(PortalPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                $"portal:{context.Connection.RemoteIpAddress}",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var key = context.User.FindFirst("sub")?.Value ?? $"ip:{context.Connection.RemoteIpAddress}";
                return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 120,
                    TokensPerPeriod = 60,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 20,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true,
                });
            });
        });

        return services;
    }
}
