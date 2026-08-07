using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A buyer's posted need. Sellers bid against it via SellerResponse. Replaces the live schema's
/// requirements table + the status transitions driven by buyer_accept_bid / finalize_bid_to_deal
/// / the cancel-cascade trigger.
/// </summary>
public class Requirement : BaseEntity, IAggregateRoot
{
    public string RequirementNumber { get; private set; } = default!;
    public Guid BuyerId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public string? Grade { get; private set; }
    public int QuantityPcs { get; private set; }
    public string DestinationCountry { get; private set; } = default!;
    public Guid? CategoryId { get; private set; }
    public RequirementStatus Status { get; private set; } = RequirementStatus.Posted;

    private Requirement()
    {
        // EF Core
    }

    public Requirement(string requirementNumber, Guid buyerId, string itemName, int quantityPcs,
        string destinationCountry, string? grade = null, Guid? categoryId = null)
    {
        if (quantityPcs <= 0)
            throw new DomainException("Requirement quantity must be greater than zero.");

        RequirementNumber = requirementNumber;
        BuyerId = buyerId;
        ItemName = itemName;
        QuantityPcs = quantityPcs;
        DestinationCountry = destinationCountry;
        Grade = grade;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Called after a bid is accepted: fully committed -> ready_to_order, otherwise -> matching.
    /// Mirrors the status-advance step in finalize_bid_to_deal.
    /// </summary>
    public void AdvanceOnAcceptedQuantity(int remainingAfterAcceptance)
    {
        if (Status is RequirementStatus.Settled or RequirementStatus.Cancelled)
            return;

        Status = remainingAfterAcceptance <= 0 ? RequirementStatus.ReadyToOrder : RequirementStatus.Matching;
    }

    /// <summary>
    /// Called by the deal-cancellation cascade when nothing is left committed on this requirement.
    /// </summary>
    public void RevertToMatching()
    {
        if (Status is RequirementStatus.Settled or RequirementStatus.Cancelled)
            return;

        Status = RequirementStatus.Matching;
    }

    public void MarkInFulfillment() => Status = RequirementStatus.InFulfillment;

    public void MarkSettled() => Status = RequirementStatus.Settled;

    public void Cancel() => Status = RequirementStatus.Cancelled;
}
