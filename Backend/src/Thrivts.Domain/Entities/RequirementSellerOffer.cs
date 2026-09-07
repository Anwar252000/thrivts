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

    public RequirementSellerOffer(string offerNumber, Guid requirementId, Guid sellerId, string itemName,
        int quantityPcs, string grade, decimal offerPricePerPc, Guid sentBy, string? adminNotes, DateTimeOffset expiresAt)
    {
        OfferNumber = offerNumber;
        RequirementId = requirementId;
        SellerId = sellerId;
        ItemName = itemName;
        QuantityPcs = quantityPcs;
        Grade = grade;
        OfferPricePerPc = offerPricePerPc;
        CurrentPricePerPc = offerPricePerPc;
        SentBy = sentBy;
        AdminNotes = adminNotes;
        ExpiresAt = expiresAt;
        SentAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Mirrors loadOffers()'s auto-mark-viewed side effect (only flips a still-Sent offer
    /// to Viewed — any later action already backfills ViewedAt itself via PostRound/Accept/Decline).</summary>
    public void MarkViewed(DateTimeOffset occurredAt)
    {
        ViewedAt ??= occurredAt;
        if (Status == OfferStatus.Sent)
            Status = OfferStatus.Viewed;
    }

    /// <summary>Mirrors post_offer_round's counter branch. CounterPricePerPc/CounterNotes only move
    /// when the SELLER is the one countering (matches the live RPC's `case when v_party='seller'`) —
    /// an admin counter only moves the standing CurrentPricePerPc.</summary>
    public void PostRound(NegotiationActor party, decimal newPricePerPc, string? notes, DateTimeOffset occurredAt)
    {
        EnsureOpen(occurredAt);
        CurrentPricePerPc = newPricePerPc;
        Status = OfferStatus.Countered;
        RespondedAt = occurredAt;
        ViewedAt ??= occurredAt;

        if (party == NegotiationActor.Seller)
        {
            CounterPricePerPc = newPricePerPc;
            CounterNotes = notes;
        }
    }

    public void Accept(DateTimeOffset occurredAt)
    {
        EnsureOpen(occurredAt);
        Status = OfferStatus.Accepted;
        RespondedAt = occurredAt;
    }

    public void Decline(DateTimeOffset occurredAt)
    {
        EnsureOpen(occurredAt);
        Status = OfferStatus.Declined;
        RespondedAt = occurredAt;
    }

    public void Expire() => Status = OfferStatus.Expired;

    public void Withdraw() => Status = OfferStatus.Withdrawn;

    public void LinkDeal(Guid dealId) => DealId = dealId;

    /// <summary>seller.html gated every offer action client-side on expires_at (never verified
    /// server-side before) — lazily flips to Expired the first time anyone tries to act on a
    /// past-expiry offer, rather than requiring a separate sweep job.</summary>
    private void EnsureOpen(DateTimeOffset occurredAt)
    {
        if (Status is OfferStatus.Accepted or OfferStatus.Declined or OfferStatus.Expired or OfferStatus.Withdrawn)
            throw new DomainException($"This offer is already {Status}.");

        if (ExpiresAt is not null && ExpiresAt < occurredAt)
        {
            Status = OfferStatus.Expired;
            throw new DomainException("This offer has expired.");
        }
    }
}
