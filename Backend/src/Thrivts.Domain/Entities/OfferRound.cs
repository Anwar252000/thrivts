using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One round of the admin/seller back-and-forth on a RequirementSellerOffer — an audit trail of
/// post_offer_round calls in the live schema. NOTE: inferred — the offer_rounds table was not in
/// the exported SQL set; verify the exact column set once the schema export lands.
/// </summary>
public class OfferRound : BaseEntity, IAggregateRoot
{
    public Guid RequirementSellerOfferId { get; private set; }
    public NegotiationActor PostedBy { get; private set; }
    public int RoundNumber { get; private set; }
    public decimal PricePerPcUsd { get; private set; }
    public string? Note { get; private set; }

    private OfferRound()
    {
        // EF Core
    }

    public OfferRound(Guid requirementSellerOfferId, NegotiationActor postedBy, int roundNumber,
        decimal pricePerPcUsd, string? note = null)
    {
        RequirementSellerOfferId = requirementSellerOfferId;
        PostedBy = postedBy;
        RoundNumber = roundNumber;
        PricePerPcUsd = pricePerPcUsd;
        Note = note;
    }
}
