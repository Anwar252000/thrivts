using Thrivts.Domain.Common;
using Thrivts.Domain.DomainServices;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per influencer. Id == the influencer's auth user id. Earns a recurring commission
/// (CommissionCalculator.DefaultInfluencerRate) on a referred buyer's orders for
/// CommissionCalculator.InfluencerCommissionWindow from that buyer's first order, via
/// ReferralCode / apply_referral_code in the live schema.
/// </summary>
public class Influencer : BaseEntity, IAggregateRoot
{
    public string FullName { get; private set; } = default!;
    public string ReferralCode { get; private set; } = default!;
    public decimal CommissionRate { get; private set; } = CommissionCalculator.DefaultInfluencerRate;
    public bool IsActive { get; private set; } = true;

    private Influencer()
    {
        // EF Core
    }

    public Influencer(Guid authUserId, string fullName, string referralCode)
    {
        Id = authUserId;
        FullName = fullName;
        ReferralCode = referralCode;
    }

    public void SetCommissionRate(decimal rate)
    {
        if (rate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(rate), "Commission rate must be between 0 and 1.");

        CommissionRate = rate;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
