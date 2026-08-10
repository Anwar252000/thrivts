using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One round of the admin/seller back-and-forth on a RequirementSellerOffer — an audit trail of
/// post_offer_round calls in the live schema.
/// </summary>
public class OfferRound : BaseEntity, IAggregateRoot
{
    public Guid OfferId { get; private set; }
    public NegotiationActor Party { get; private set; }

    /// <summary>Free-text action label ('offer' | 'counter' | 'accept' | 'decline', etc.) — no dedicated enum in the live schema.</summary>
    public string Kind { get; private set; } = default!;
    public decimal? PricePerPcUsd { get; private set; }
    public string? Notes { get; private set; }
    public Guid? CreatedBy { get; private set; }

    private OfferRound()
    {
        // EF Core
    }

    public OfferRound(Guid offerId, NegotiationActor party, string kind, decimal? pricePerPcUsd = null,
        string? notes = null, Guid? createdBy = null)
    {
        OfferId = offerId;
        Party = party;
        Kind = kind;
        PricePerPcUsd = pricePerPcUsd;
        Notes = notes;
        CreatedBy = createdBy;
    }
}
