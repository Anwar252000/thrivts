using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An admin-initiated direct offer to a specific seller on a Requirement (post_offer_round /
/// admin_accept_offer / admin_decline_offer / accept_seller_offer). Accepting an offer does NOT
/// itself create a deal — admin still runs the separate deal-from-match step
/// (THRIVTS_FLOW_MATRIX.md §5).
/// </summary>
public class RequirementSellerOffer : BaseEntity, IAggregateRoot
{
    public string? OfferNumber { get; private set; }
    public Guid RequirementId { get; private set; }
    public Guid SellerId { get; private set; }
    public string? ItemName { get; private set; }
    public int? QuantityPcs { get; private set; }
    public string? Grade { get; private set; }

    public decimal? OfferPricePerPc { get; private set; }
    public string OfferCurrency { get; private set; } = "USD";
    public decimal? OfferPriceUsd { get; private set; }
    public decimal? CurrentPricePerPc { get; private set; }
    public string? ExchangeRateSnapshotJson { get; private set; }

    public OfferStatus Status { get; private set; } = OfferStatus.Sent;
    public decimal? CounterPricePerPc { get; private set; }
    public string? CounterCurrency { get; private set; }
    public string? CounterNotes { get; private set; }
    public string? AdminNotes { get; private set; }

    public Guid? SentBy { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ViewedAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public Guid? DealId { get; private set; }

    private RequirementSellerOffer()
    {
        // EF Core
    }

    public RequirementSellerOffer(Guid requirementId, Guid sellerId, decimal offerPricePerPc, Guid sentBy)
    {
        RequirementId = requirementId;
        SellerId = sellerId;
        OfferPricePerPc = offerPricePerPc;
        CurrentPricePerPc = offerPricePerPc;
        SentBy = sentBy;
        SentAt = DateTimeOffset.UtcNow;
    }

    public void MarkViewed(DateTimeOffset occurredAt) => ViewedAt ??= occurredAt;

    public void PostRound(decimal newPricePerPc, DateTimeOffset occurredAt)
    {
        EnsureOpen();
        CurrentPricePerPc = newPricePerPc;
        Status = OfferStatus.Countered;
        RespondedAt = occurredAt;
    }

    public void Accept(DateTimeOffset occurredAt)
    {
        EnsureOpen();
        Status = OfferStatus.Accepted;
        RespondedAt = occurredAt;
    }

    public void Decline(DateTimeOffset occurredAt)
    {
        EnsureOpen();
        Status = OfferStatus.Declined;
        RespondedAt = occurredAt;
    }

    public void Expire() => Status = OfferStatus.Expired;

    public void Withdraw() => Status = OfferStatus.Withdrawn;

    public void LinkDeal(Guid dealId) => DealId = dealId;

    private void EnsureOpen()
    {
        if (Status is OfferStatus.Accepted or OfferStatus.Declined or OfferStatus.Expired or OfferStatus.Withdrawn)
            throw new DomainException($"This offer is already {Status}.");
    }
}
