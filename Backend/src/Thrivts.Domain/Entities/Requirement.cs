using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A buyer's posted need. Sellers bid against it via SellerResponse. Some fields duplicate an
/// older naming (e.g. TargetPricePerPiece vs TargetPricePerPc) — the live schema accumulated both
/// across migrations; kept for round-trip fidelity, not because both are actively used.
/// </summary>
public class Requirement : BaseEntity, IAggregateRoot
{
    public string RequirementNumber { get; private set; } = default!;
    public Guid BuyerId { get; private set; }
    public int? CategoryId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public int QuantityPcs { get; private set; }
    public GradeType Grade { get; private set; }

    public decimal BuyerTargetPriceUsd { get; private set; }
    public CurrencyType BuyerCurrency { get; private set; } = CurrencyType.USD;
    public decimal? BuyerTargetPriceOriginal { get; private set; }
    public decimal? BuyerExchangeRate { get; private set; }
    public decimal? SellerTargetPriceUsd { get; private set; }
    public decimal? SpreadPerPcUsd { get; private set; }
    public decimal? TotalSpreadUsd { get; private set; }

    public string DestinationCountry { get; private set; } = default!;
    public string? DestinationPort { get; private set; }
    public int? DeliveryTimelineDays { get; private set; }
    public DateOnly? PreferredDispatchDate { get; private set; }
    public DateOnly? PreferredShipmentDate { get; private set; }
    public string? ShippingMode { get; private set; }
    public string? SpecificBrands { get; private set; }
    public decimal? EstimatedWeightKg { get; private set; }

    public RequirementStatus Status { get; private set; } = RequirementStatus.PendingReview;
    public RequirementType? RequirementType { get; private set; }
    public bool PublicDisplay { get; private set; }
    public string? BuyerNotes { get; private set; }
    public string? AdminNotes { get; private set; }
    public string? AdditionalNotes { get; private set; }

    public SellerTier MinSellerTier { get; private set; } = SellerTier.Bronze;
    public string[]? RestrictedToTags { get; private set; }

    public string? TargetCurrency { get; private set; }
    public decimal? TargetPricePerPiece { get; private set; }
    public decimal? TargetPricePerPc { get; private set; }
    public string? TargetPriceCurrency { get; private set; }
    public decimal? TargetTotalBuyerCurrency { get; private set; }
    public decimal? TargetTotalUsd { get; private set; }
    public string? ExchangeRateSnapshotJson { get; private set; }

    public DateTimeOffset? PostedAt { get; private set; }
    public DateTimeOffset? MatchedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private Requirement()
    {
        // EF Core
    }

    public Requirement(string requirementNumber, Guid buyerId, string itemName, int quantityPcs, GradeType grade,
        decimal buyerTargetPriceUsd, string destinationCountry, int? categoryId = null)
    {
        if (quantityPcs <= 0)
            throw new DomainException("Requirement quantity must be greater than zero.");

        RequirementNumber = requirementNumber;
        BuyerId = buyerId;
        ItemName = itemName;
        QuantityPcs = quantityPcs;
        Grade = grade;
        BuyerTargetPriceUsd = buyerTargetPriceUsd;
        DestinationCountry = destinationCountry;
        CategoryId = categoryId;
    }

    public void Post(DateTimeOffset occurredAt)
    {
        Status = RequirementStatus.Posted;
        PostedAt = occurredAt;
        PublicDisplay = true;
    }

    public void SetPublicDisplay(bool publicDisplay) => PublicDisplay = publicDisplay;

    /// <summary>Admin edit of the core listing fields (replaces the direct requirements.update call).</summary>
    public void UpdateDetails(string itemName, int quantityPcs, GradeType grade, string destinationCountry,
        decimal buyerTargetPriceUsd, string? adminNotes)
    {
        if (quantityPcs <= 0)
            throw new DomainException("Requirement quantity must be greater than zero.");

        ItemName = itemName;
        QuantityPcs = quantityPcs;
        Grade = grade;
        DestinationCountry = destinationCountry;
        BuyerTargetPriceUsd = buyerTargetPriceUsd;
        AdminNotes = adminNotes;
    }

    /// <summary>Replaces editRequirementTagsAndTier() — controls which sellers see this
    /// requirement at all (empty/null tags = matched by tier alone).</summary>
    public void SetMatchingFilters(SellerTier minSellerTier, string[]? restrictedToTags)
    {
        MinSellerTier = minSellerTier;
        RestrictedToTags = restrictedToTags;
    }

    /// <summary>Called after a bid is accepted: fully committed -> ready_to_order, otherwise -> matching.</summary>
    public void AdvanceOnAcceptedQuantity(int remainingAfterAcceptance, DateTimeOffset occurredAt)
    {
        if (IsTerminal()) return;

        if (remainingAfterAcceptance <= 0)
        {
            Status = RequirementStatus.ReadyToOrder;
            MatchedAt = occurredAt;
        }
        else
        {
            Status = RequirementStatus.Matching;
        }
    }

    /// <summary>Called by the deal-cancellation cascade when nothing is left committed on this requirement.</summary>
    public void RevertToMatching()
    {
        if (IsTerminal()) return;
        Status = RequirementStatus.Matching;
    }

    public void MarkInFulfillment() => Status = RequirementStatus.InFulfillment;

    public void MarkSettled(DateTimeOffset occurredAt)
    {
        Status = RequirementStatus.Settled;
        SettledAt = occurredAt;
    }

    public void Cancel() => Status = RequirementStatus.Cancelled;

    public void Expire() => Status = RequirementStatus.Expired;

    private bool IsTerminal() => Status is RequirementStatus.Settled or RequirementStatus.Cancelled;
}
