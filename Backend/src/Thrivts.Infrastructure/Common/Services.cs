using Microsoft.Extensions.Caching.Hybrid;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Common;

public sealed class KarachiClock(TimeProvider timeProvider) : IClock
{
    private static readonly TimeZoneInfo Karachi = ResolveZone();

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today => ToBusinessDate(UtcNow);

    public DateOnly ToBusinessDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Karachi).DateTime);

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Karachi", "Pakistan Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
        }

        return TimeZoneInfo.CreateCustomTimeZone("PKT", TimeSpan.FromHours(5), "Pakistan Time", "PKT");
    }
}

/// <summary>Reference data (catalogue, suppliers, sack sizes) is read on almost every screen and
/// changes rarely — served from an in-process cache, invalidated by tag on every write.</summary>
public sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    private static readonly HybridCacheEntryOptions Entry = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, string[] tags, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(key, factory, Entry, tags, cancellationToken);

    public ValueTask InvalidateAsync(string tag, CancellationToken cancellationToken) =>
        cache.RemoveByTagAsync(tag, cancellationToken);
}
