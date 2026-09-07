using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per seller. Id == the seller's auth user id (same convention as Profile). Approval/
/// active-status lifecycle lives on Profile — THRIVTS_FLOW_MATRIX.md §1.4.
/// PublicAlias is the ONLY identifier a buyer may ever see (the anonymity "moat" — see
/// README_HANDOVER.md). CompanyName/Phone/WhatsApp/ReferenceContact must never be serialized
/// into a buyer-facing DTO; only AdminController-authorized responses may include them.
/// </summary>
public class Seller : BaseEntity, IAggregateRoot
{
    public string? CompanyName { get; private set; }
    public string LocationCity { get; private set; } = default!;
    public string LocationCountry { get; private set; } = "Pakistan";
    public int? YearsInBusiness { get; private set; }
    public string? SocialMediaJson { get; private set; }
    public string[] CategoriesSupplied { get; private set; } = [];
    public int? MonthlyVolumeCapacityPcs { get; private set; }
    public string? ReferenceContact { get; private set; }
    public string? Phone { get; private set; }
    public string? WhatsApp { get; private set; }

    public SellerTier Tier { get; private set; } = SellerTier.Bronze;
    public DateTimeOffset TierUpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? TierUpdatedBy { get; private set; }
    public string? TierNotes { get; private set; }

    /// <summary>Admin-managed tag list controlling which requirements a seller can see (current_seller_tags()).</summary>
    public string[]? Tags { get; private set; }
    public string[]? ManualTags { get; private set; }

    public string? SellerCode { get; private set; }
    public string PublicAlias { get; private set; } = default!;

    public bool KycVerified { get; private set; }
    public DateTimeOffset? KycVerifiedAt { get; private set; }
    public Guid? KycVerifiedBy { get; private set; }
    public string? KycNotes { get; private set; }

    public Guid? InvitedBy { get; private set; }
    public DateTimeOffset? InvitedAt { get; private set; }
    public string? InviteToken { get; private set; }
    public DateTimeOffset? InviteAcceptedAt { get; private set; }

    public bool IsActive { get; private set; } = true;
    public bool IsPreLoaded { get; private set; }
    public DateTimeOffset? LastActiveAt { get; private set; }

    public int TotalOrdersFulfilled { get; private set; }
    public long TotalPcsSupplied { get; private set; }
    public decimal TotalPaidUsd { get; private set; }
    public int DisputeCount { get; private set; }
    public int BackoutCount { get; private set; }
    public decimal? OnTimeDispatchRate { get; private set; }
    public decimal? AvgGradeAccuracy { get; private set; }
    public string? Notes { get; private set; }

    private Seller()
    {
        // EF Core
    }

    public Seller(Guid authUserId, string publicAlias, string locationCity, string[] categoriesSupplied, string locationCountry = "Pakistan")
    {
        Id = authUserId;
        PublicAlias = publicAlias;
        LocationCity = locationCity;
        LocationCountry = locationCountry;
        CategoriesSupplied = categoriesSupplied;
    }

    public void VerifyKyc(Guid verifiedBy, DateTimeOffset occurredAt, string? notes = null)
    {
        KycVerified = true;
        KycVerifiedBy = verifiedBy;
        KycVerifiedAt = occurredAt;
        KycNotes = notes;
    }

    public void UnverifyKyc() => KycVerified = false;

    public void SetTier(SellerTier tier, Guid updatedBy, DateTimeOffset occurredAt, string? notes = null)
    {
        Tier = tier;
        TierUpdatedBy = updatedBy;
        TierUpdatedAt = occurredAt;
        TierNotes = notes;
    }

    public void SetTags(string[]? tags) => Tags = tags;

    public void UpdateContactDetails(string? phone, string? whatsApp, string? referenceContact)
    {
        Phone = phone;
        WhatsApp = whatsApp;
        ReferenceContact = referenceContact;
    }

    /// <summary>Sets the signup-form fields the constructor doesn't cover — called once, right
    /// after construction, by RegisterSellerCommand (mirrors Buyer.CompleteSignupProfile).</summary>
    public void CompleteSignupProfile(string? companyName, string? phone, string? whatsApp,
        int? yearsInBusiness, int? monthlyVolumeCapacityPcs, string? socialMediaJson)
    {
        CompanyName = companyName;
        Phone = phone;
        WhatsApp = whatsApp;
        YearsInBusiness = yearsInBusiness;
        MonthlyVolumeCapacityPcs = monthlyVolumeCapacityPcs;
        SocialMediaJson = socialMediaJson;
    }

    public void RecordActivity(DateTimeOffset occurredAt) => LastActiveAt = occurredAt;

    public void RecordFulfilledOrder(long quantityPcs, decimal paidUsd)
    {
        TotalOrdersFulfilled += 1;
        TotalPcsSupplied += quantityPcs;
        TotalPaidUsd += paidUsd;
    }

    public void RecordDispute() => DisputeCount += 1;

    public void RecordBackout() => BackoutCount += 1;
}
