using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Thrivts.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string PublicPolicy = "public";

    /// <summary>
    /// Applies to the anonymous-allowed endpoints only (public activity feed, partner
    /// applications, referral codes) — attach via [EnableRateLimiting(RateLimitingExtensions.PublicPolicy)]
    /// on PublicController actions. Authenticated endpoints are not rate-limited here.
    /// </summary>
    public static IServiceCollection AddThrivtsRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(PublicPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = 30;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        return services;
    }
}
