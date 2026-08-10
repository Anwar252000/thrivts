using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// Cached platform-wide aggregate stats for dashboards (the live schema's platform_stats table —
/// a singleton row, `id` defaults to 1 and nothing else ever writes a second row).
/// </summary>
public class PlatformStats : IAggregateRoot
{
    public int Id { get; private set; } = 1;

    public int TotalRequirementsPosted { get; private set; }
    public int TotalDealsClosed { get; private set; }
    public long TotalPcsMoved { get; private set; }
    public decimal TotalVolumeUsd { get; private set; }
    public int ActiveBuyers { get; private set; }
    public int ActiveSellers { get; private set; }
    public int ActiveAgencies { get; private set; }
    public int CountriesServed { get; private set; }

    public int TodayRequirements { get; private set; }
    public int TodayMatches { get; private set; }
    public int TodayDispatched { get; private set; }
    public int ThisMonthDeals { get; private set; }
    public decimal ThisMonthVolumeUsd { get; private set; }

    public DateTimeOffset LastUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PlatformStats()
    {
        // EF Core
    }

    public void Refresh(int totalRequirementsPosted, int totalDealsClosed, long totalPcsMoved, decimal totalVolumeUsd,
        int activeBuyers, int activeSellers, int activeAgencies, int countriesServed,
        int todayRequirements, int todayMatches, int todayDispatched, int thisMonthDeals, decimal thisMonthVolumeUsd,
        DateTimeOffset occurredAt)
    {
        TotalRequirementsPosted = totalRequirementsPosted;
        TotalDealsClosed = totalDealsClosed;
        TotalPcsMoved = totalPcsMoved;
        TotalVolumeUsd = totalVolumeUsd;
        ActiveBuyers = activeBuyers;
        ActiveSellers = activeSellers;
        ActiveAgencies = activeAgencies;
        CountriesServed = countriesServed;
        TodayRequirements = todayRequirements;
        TodayMatches = todayMatches;
        TodayDispatched = todayDispatched;
        ThisMonthDeals = thisMonthDeals;
        ThisMonthVolumeUsd = thisMonthVolumeUsd;
        LastUpdatedAt = occurredAt;
    }
}
