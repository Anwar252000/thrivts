using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A seller's bid against a Requirement — the core of the bidding/negotiation loop (the live
/// schema's seller_responses table). Replaces buyer_counter_bid, seller_respond_to_counter and
/// finalize_bid_to_deal's bid-side mutations.
///
/// Money model (flat platform fee, frozen at bid time — see FeePerPcAppliedUsd):
///   buyer sees   = CurrentPriceUsd + FeePerPcAppliedUsd   (the fee IS the spread)
///   seller sees  = CurrentPriceUsd (their own price) and the fee, never the buyer's raw number
/// The buyer-facing DTO must expose ONLY the combined price — never CurrentPriceUsd/ProposedPriceUsd/
/// FeePerPcAppliedUsd individually. That split is the second moat rule (fee opacity).
/// </summary>
public class SellerResponse : BaseEntity, IAggregateRoot
{
    public Guid RequirementId { get; private set; }
    public Guid SellerId { get; private set; }
    public int AvailableQuantityPcs { get; private set; }

    /// <summary>The seller's originally-submitted price/pc (USD, seller-side, never shown to the buyer directly).</summary>
    public decimal? ProposedPriceUsd { get; private set; }

    /// <summary>The live seller-side price/pc on the table right now — updated by counters.</summary>
    public decimal? CurrentPriceUsd { get; private set; }

    /// <summary>Platform fee/pc frozen at insert. Never recalculated even if the global rate changes later.</summary>
    public decimal? FeePerPcAppliedUsd { get; private set; }

    public string? SellerNotes { get; private set; }
    public BidStatus Status { get; private set; } = BidStatus.Pending;
    public NegotiationState NegotiationState { get; private set; } = NegotiationState.Open;
    public NegotiationActor? LastActor { get; private set; }
    public int RoundCount { get; private set; }
    public DateTimeOffset? LastActionAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }

    public int? AcceptedQuantityPcs { get; private set; }
    public decimal? FinalPriceUsd { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? BackedOutAt { get; private set; }
    public string? BackoutReason { get; private set; }

    public decimal? BuyerCounterPriceUsd { get; private set; }
    public DateTimeOffset? BuyerCounterAt { get; private set; }
    public string? BuyerCounterNote { get; private set; }

    public Guid? DealId { get; private set; }

    private SellerResponse()
    {
        // EF Core
    }

    public SellerResponse(Guid requirementId, Guid sellerId, int availableQuantityPcs,
        decimal proposedPriceUsd, decimal feePerPcAppliedUsd, string? sellerNotes = null)
    {
        if (availableQuantityPcs <= 0)
            throw new DomainException("Bid quantity must be greater than zero.");
        if (proposedPriceUsd <= 0)
            throw new DomainException("Bid price must be greater than zero.");

        RequirementId = requirementId;
        SellerId = sellerId;
        AvailableQuantityPcs = availableQuantityPcs;
        ProposedPriceUsd = proposedPriceUsd;
        CurrentPriceUsd = proposedPriceUsd;
        FeePerPcAppliedUsd = feePerPcAppliedUsd;
        SellerNotes = sellerNotes;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Buyer-facing all-in price/pc — the ONLY number a buyer-side DTO may carry.</summary>
    public decimal? BuyerPricePerPcUsd => CurrentPriceUsd + FeePerPcAppliedUsd;

    /// <summary>Seller re-quotes on a bid they already own (replaces seller_revise_bid — the old
    /// duplicate-key-erroring re-submit). Resets to a fresh seller offer and clears any stale buyer
    /// counter, exactly as that RPC's own comment describes.</summary>
    public void Revise(decimal newPriceUsd, int availableQuantityPcs, string? sellerNotes)
    {
        EnsureNotAccepted();

        ProposedPriceUsd = newPriceUsd;
        CurrentPriceUsd = newPriceUsd;
        AvailableQuantityPcs = availableQuantityPcs;
        SellerNotes = sellerNotes;
        Status = BidStatus.Pending;
        NegotiationState = NegotiationState.CounteredBySeller;
        BuyerCounterPriceUsd = null;
        BuyerCounterAt = null;
        BuyerCounterNote = null;
        Touch(NegotiationActor.Seller);
    }

    /// <summary>Buyer counters with a fee-inclusive price; stored back out as the seller-side number.</summary>
    public void BuyerCounter(decimal counterBuyerPriceUsd, string? note)
    {
        EnsureNotAccepted();
        if (counterBuyerPriceUsd <= 0)
            throw new DomainException("Enter a valid counter price.");

        var fee = FeePerPcAppliedUsd ?? 0;
        var sellerSide = counterBuyerPriceUsd - fee;
        if (sellerSide <= 0)
            throw new DomainException("Counter must be more than the platform fee.");

        BuyerCounterPriceUsd = sellerSide;
        BuyerCounterAt = DateTimeOffset.UtcNow;
        BuyerCounterNote = note;
        CurrentPriceUsd = sellerSide;
        NegotiationState = NegotiationState.CounteredByBuyer;
        Touch(NegotiationActor.Buyer);
    }

    /// <summary>Seller counters back with their own (seller-side) price.</summary>
    public void SellerCounter(decimal newPriceUsd, string? note)
    {
        EnsureNotAccepted();
        if (newPriceUsd <= 0)
            throw new DomainException("Enter a valid counter price.");

        CurrentPriceUsd = newPriceUsd;
        ProposedPriceUsd = newPriceUsd;
        SellerNotes = note ?? SellerNotes;
        NegotiationState = NegotiationState.CounteredBySeller;
        Touch(NegotiationActor.Seller);
    }

    public void DeclineCounter()
    {
        EnsureNotAccepted();
        NegotiationState = NegotiationState.Declined;
        Touch(NegotiationActor.Seller);
    }

    /// <summary>
    /// Seller accepts the buyer's outstanding counter — mutual agreement. The caller (application
    /// layer) must follow this with MarkAccepted(dealId) once the Deal aggregate is created; this
    /// method only settles the price, it does not cross the aggregate boundary to create the deal.
    /// </summary>
    public void AcceptBuyerCounter()
    {
        EnsureNotAccepted();
        if (BuyerCounterPriceUsd is null)
            throw new DomainException("There is no buyer counter to accept.");

        CurrentPriceUsd = BuyerCounterPriceUsd.Value;
        ProposedPriceUsd = BuyerCounterPriceUsd.Value;
        // Mirrors seller_respond_to_counter's accept branch: back to 'open' at the now-agreed
        // price (the ball moves to the buyer to confirm), and the stale counter is cleared so the
        // seller's own board stops showing "Buyer countered — your move" forever.
        NegotiationState = NegotiationState.Open;
        BuyerCounterPriceUsd = null;
        BuyerCounterAt = null;
        BuyerCounterNote = null;
        Touch(NegotiationActor.Seller);
    }

    /// <summary>Called once a Deal has been created from this bid (either accept path).</summary>
    public void MarkAccepted(Guid dealId, int acceptedQuantityPcs, decimal finalPriceUsd, DateTimeOffset occurredAt)
    {
        Status = BidStatus.Accepted;
        NegotiationState = NegotiationState.Accepted;
        DealId = dealId;
        AcceptedQuantityPcs = acceptedQuantityPcs;
        FinalPriceUsd = finalPriceUsd;
        AcceptedAt = occurredAt;
        LastActionAt = occurredAt;
    }

    /// <summary>Called by the deal-cancellation cascade to free this bid back onto the board.</summary>
    public void ReleaseFromCancelledDeal()
    {
        Status = BidStatus.Pending;
        NegotiationState = NegotiationState.Open;
        DealId = null;
        LastActionAt = DateTimeOffset.UtcNow;
    }

    public void BackOut(string reason, DateTimeOffset occurredAt)
    {
        Status = BidStatus.CancelledBackout;
        BackoutReason = reason;
        BackedOutAt = occurredAt;
    }

    public bool CanBeAccepted() => Status == BidStatus.Pending;

    private void EnsureNotAccepted()
    {
        if (Status == BidStatus.Accepted || NegotiationState == NegotiationState.Accepted)
            throw new DomainException("This bid is already accepted.");
    }

    private void Touch(NegotiationActor actor)
    {
        LastActor = actor;
        RoundCount += 1;
        LastActionAt = DateTimeOffset.UtcNow;
    }
}
