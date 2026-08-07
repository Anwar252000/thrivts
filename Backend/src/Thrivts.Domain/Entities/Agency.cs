using Thrivts.Domain.Common;
using Thrivts.Domain.DomainServices;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per agency. Id == the agency's auth user id. An agency earns a commission
/// (CommissionCalculator.DefaultAgencyRate of the deal spread) on deals it referred, via
/// ReferralCode / apply_agency_ref in the live schema.
/// </summary>
public class Agency : BaseEntity, IAggregateRoot
{
    public string CompanyName { get; private set; } = default!;
    public string ReferralCode { get; private set; } = default!;
    public decimal CommissionRate { get; private set; } = CommissionCalculator.DefaultAgencyRate;
    public bool IsActive { get; private set; } = true;

    private Agency()
    {
        // EF Core
    }

    public Agency(Guid authUserId, string companyName, string referralCode)
    {
        Id = authUserId;
        CompanyName = companyName;
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
