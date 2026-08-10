using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per agency. Id == the agency's auth user id. Earns a commission on deals it referred
/// via ReferralCode / apply_agency_ref in the live schema.
/// NOTE: CommissionRate is stored as a PERCENTAGE (DB default 30.00, i.e. 30%), not the 0-1
/// fraction CommissionCalculator.DefaultAgencyRate uses — divide by 100 before calculating.
/// </summary>
public class Agency : BaseEntity, IAggregateRoot
{
    public string AgencyName { get; private set; } = default!;
    public string AgencyCode { get; private set; } = default!;
    public string OwnerFullName { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public string? City { get; private set; }
    public int? TeamSize { get; private set; }
    public decimal CommissionRate { get; private set; } = 30.00m;

    public int TotalBuyersReferred { get; private set; }
    public int TotalDealsClosed { get; private set; }
    public decimal TotalCommissionEarnedUsd { get; private set; }
    public decimal TotalCommissionPaidUsd { get; private set; }
    public decimal TotalCommissionPendingUsd { get; private set; }

    public DateTimeOffset? ContractSignedAt { get; private set; }
    public string? ContractDocumentUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Plain text on this table (not the profiles.approval_status enum) — defaults 'approved'.</summary>
    public string ApprovalStatus { get; private set; } = "approved";
    public string? Notes { get; private set; }

    private Agency()
    {
        // EF Core
    }

    public Agency(Guid authUserId, string agencyName, string agencyCode, string ownerFullName, string country)
    {
        Id = authUserId;
        AgencyName = agencyName;
        AgencyCode = agencyCode;
        OwnerFullName = ownerFullName;
        Country = country;
    }

    public void SetCommissionRate(decimal ratePercent)
    {
        if (ratePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(ratePercent), "Commission rate must be a percentage between 0 and 100.");

        CommissionRate = ratePercent;
    }

    public void RecordReferredBuyer() => TotalBuyersReferred += 1;

    public void RecordClosedDeal(decimal commissionEarnedUsd)
    {
        TotalDealsClosed += 1;
        TotalCommissionEarnedUsd += commissionEarnedUsd;
        TotalCommissionPendingUsd += commissionEarnedUsd;
    }

    public void RecordCommissionPaid(decimal amountUsd)
    {
        TotalCommissionPaidUsd += amountUsd;
        TotalCommissionPendingUsd = Math.Max(TotalCommissionPendingUsd - amountUsd, 0);
    }

    public void SignContract(DateTimeOffset signedAt, string? documentUrl)
    {
        ContractSignedAt = signedAt;
        ContractDocumentUrl = documentUrl;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
