using Thrivts.Domain.ValueObjects;

namespace Thrivts.Domain.DomainServices;

/// <summary>
/// Agency commission = agencyRate * deal spread (default 30%, per agency.commission_rate).
/// Influencer commission = influencerRate * order value, recurring for 12 months from a buyer's
/// first order (default 5%, per influencer.commission_rate).
/// Accrues on Delivered, releases on Settled (see Deal.AdvanceTo).
/// NOTE: exact percentages/timing must be reconfirmed with the business owner before go-live —
/// these are the values documented pre-migration, not yet re-verified against the live DB.
/// </summary>
public static class CommissionCalculator
{
    public const decimal DefaultAgencyRate = 0.30m;
    public const decimal DefaultInfluencerRate = 0.05m;
    public static readonly TimeSpan InfluencerCommissionWindow = TimeSpan.FromDays(365);
    public static readonly TimeSpan CommissionReleaseDelay = TimeSpan.FromDays(20);

    public static Money CalculateAgencyCommission(Money dealSpread, decimal agencyRate)
    {
        if (agencyRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(agencyRate), "Commission rate must be between 0 and 1.");

        return new Money(dealSpread.Amount * agencyRate, dealSpread.Currency);
    }

    public static Money CalculateInfluencerCommission(Money orderValue, decimal influencerRate) =>
        new(orderValue.Amount * influencerRate, orderValue.Currency);
}
