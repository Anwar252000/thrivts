using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An admin-initiated direct offer to a specific seller on a Requirement (the live schema's
/// requirement_seller_offers table, driven by post_offer_round / admin_accept_offer /
/// admin_decline_offer / accept_seller_offer). Accepting an offer does NOT itself create a
/// deal — admin still runs the separate deal-from-match step (THRIVTS_FLOW_MATRIX.md §5).
/// </summary>
public class RequirementSellerOffer : BaseEntity, IAggregateRoot
{
    public Guid RequirementId { get; private set; }
    public Guid SellerId { get; private set; }
    public decimal CurrentPricePerPcUsd { get; private set; }
    public OfferStatus Status { get; private set; } = OfferStatus.Pending;

    private RequirementSellerOffer()
    {
        // EF Core
    }

    public RequirementSellerOffer(Guid requirementId, Guid sellerId, decimal initialPricePerPcUsd)
    {
        RequirementId = requirementId;
        SellerId = sellerId;
        CurrentPricePerPcUsd = initialPricePerPcUsd;
    }

    public void PostRound(decimal newPricePerPcUsd)
    {
        EnsureOpen();
        CurrentPricePerPcUsd = newPricePerPcUsd;
        Status = OfferStatus.Countered;
    }

    public void Accept()
    {
        EnsureOpen();
        Status = OfferStatus.Accepted;
    }

    public void Decline()
    {
        EnsureOpen();
        Status = OfferStatus.Declined;
    }

    private void EnsureOpen()
    {
        if (Status is OfferStatus.Accepted or OfferStatus.Declined)
            throw new DomainException($"This offer is already {Status}.");
    }
}
