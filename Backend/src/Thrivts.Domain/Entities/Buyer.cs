using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per buyer. Id == the buyer's auth user id (same convention as Profile). Unlike Seller,
/// a buyer's identity is not hidden from the platform — only from other sellers/buyers on the board.
/// </summary>
public class Buyer : BaseEntity, IAggregateRoot
{
    public string CompanyName { get; private set; } = default!;
    public string? CompanyRegistration { get; private set; }
    public string? VatId { get; private set; }
    public string Country { get; private set; } = default!;
    public string? City { get; private set; }
    public string? Website { get; private set; }
    public string? Instagram { get; private set; }

    /// <summary>Raw JSON — {platform: handle} pairs. Not modelled as a value object; low query value.</summary>
    public string? SocialMediaJson { get; private set; }

    public int? EstimatedMonthlyVolumePcs { get; private set; }
    public string? MonthlyVolume { get; private set; }
    public string? TypicalRequirementType { get; private set; }

    /// <summary>Raw JSON array — see also CategoriesOfInterest (the text[] the DB actually filters on).</summary>
    public string? CategoriesJson { get; private set; }
    public string[]? CategoriesOfInterest { get; private set; }

    public Guid? AttributedToAgency { get; private set; }
    public DateTimeOffset? AttributionLockedAt { get; private set; }
    public string? AgencyRef { get; private set; }

    public Guid? InfluencerId { get; private set; }
    public string? ReferralCodeUsed { get; private set; }
    public DateTimeOffset? ReferralLinkedAt { get; private set; }
    public DateTimeOffset? FirstOrderAt { get; private set; }
    public bool FirstOrderDiscountApplied { get; private set; }

    public int TotalOrders { get; private set; }
    public decimal TotalSpendUsd { get; private set; }
    public bool IsPremium { get; private set; }
    public string? Notes { get; private set; }

    private Buyer()
    {
        // EF Core
    }

    public Buyer(Guid authUserId, string companyName, string country)
    {
        Id = authUserId;
        CompanyName = companyName;
        Country = country;
    }

    public void UpdateProfile(string companyName, string country, string? city, string? website, string? instagram)
    {
        CompanyName = companyName;
        Country = country;
        City = city;
        Website = website;
        Instagram = instagram;
    }

    public void AttributeToAgency(Guid agencyProfileId, string? agencyRef, DateTimeOffset occurredAt)
    {
        AttributedToAgency = agencyProfileId;
        AgencyRef = agencyRef;
        AttributionLockedAt = occurredAt;
    }

    public void LinkReferral(Guid influencerId, string referralCode, DateTimeOffset occurredAt)
    {
        InfluencerId = influencerId;
        ReferralCodeUsed = referralCode;
        ReferralLinkedAt = occurredAt;
    }

    public void RecordFirstOrder(DateTimeOffset occurredAt, bool discountApplied)
    {
        FirstOrderAt = occurredAt;
        FirstOrderDiscountApplied = discountApplied;
    }

    public void RecordOrder(decimal orderValueUsd)
    {
        TotalOrders += 1;
        TotalSpendUsd += orderValueUsd;
    }

    public void SetPremium(bool isPremium) => IsPremium = isPremium;
}
