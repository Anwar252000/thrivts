using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per influencer. Unlike Buyer/Seller/Agency, Id is its own generated key — UserId is an
/// optional, unenforced link to a profile (an influencer doesn't necessarily have a login).
/// Earns a recurring commission (fraction, DB default 0.05 = 5%) on a referred buyer's orders for
/// WindowMonths from that buyer's first order, via ReferralCode / apply_referral_code.
/// </summary>
public class Influencer : BaseEntity, IAggregateRoot
{
    public Guid? UserId { get; private set; }
    public string InfluencerCode { get; private set; } = default!;
    public string ReferralCode { get; private set; } = default!;
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? InstagramHandle { get; private set; }
    public string? TiktokHandle { get; private set; }
    public decimal CommissionRate { get; private set; } = 0.05m;
    public int WindowMonths { get; private set; } = 12;
    public string Status { get; private set; } = "active";

    private Influencer()
    {
        // EF Core
    }

    public Influencer(string influencerCode, string referralCode, Guid? userId = null, string? fullName = null)
    {
        InfluencerCode = influencerCode;
        ReferralCode = referralCode;
        UserId = userId;
        FullName = fullName;
    }

    public void SetCommissionRate(decimal rate)
    {
        if (rate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(rate), "Commission rate must be a fraction between 0 and 1.");

        CommissionRate = rate;
    }

    public void UpdateContactDetails(string? email, string? phone, string? instagramHandle, string? tiktokHandle)
    {
        Email = email;
        Phone = phone;
        InstagramHandle = instagramHandle;
        TiktokHandle = tiktokHandle;
    }

    public void Deactivate() => Status = "inactive";

    public void Reactivate() => Status = "active";
}
