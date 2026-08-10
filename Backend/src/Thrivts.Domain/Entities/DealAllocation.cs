using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>One seller's committed slice of a Deal (the live schema's deal_allocations table).</summary>
public class DealAllocation : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid? SellerResponseId { get; private set; }
    public Guid? SourceResponseId { get; private set; }
    public Guid? SourceOfferId { get; private set; }
    public string LotNumber { get; private set; } = default!;
    public int AllocatedQuantityPcs { get; private set; }
    public decimal PricePerPcUsd { get; private set; }

    public BidStatus Status { get; private set; } = BidStatus.Accepted;
    public Guid? ReplacedByAllocationId { get; private set; }
    public DateTimeOffset? BackedOutAt { get; private set; }

    public decimal? GrossPayoutUsd { get; private set; }
    public decimal? FeePerPcAppliedUsd { get; private set; }
    public decimal? PlatformFeeUsd { get; private set; }
    public decimal TotalPayoutUsd { get; private set; }
    public DateTimeOffset? PayoutScheduledAt { get; private set; }
    public DateTimeOffset? PayoutPaidAt { get; private set; }
    public string? PayoutMethod { get; private set; }
    public string? PayoutReference { get; private set; }

    private DealAllocation()
    {
        // EF Core
    }

    public DealAllocation(Guid dealId, Guid sellerId, string lotNumber, int allocatedQuantityPcs,
        decimal pricePerPcUsd, decimal totalPayoutUsd, Guid? sellerResponseId = null)
    {
        DealId = dealId;
        SellerId = sellerId;
        LotNumber = lotNumber;
        AllocatedQuantityPcs = allocatedQuantityPcs;
        PricePerPcUsd = pricePerPcUsd;
        TotalPayoutUsd = totalPayoutUsd;
        SellerResponseId = sellerResponseId;
    }

    /// <summary>Freezes the platform fee onto this allocation (replaces apply_allocation_fee). Net payout = gross - fee.</summary>
    public void FreezeFee(decimal feePerPcAppliedUsd)
    {
        var gross = AllocatedQuantityPcs * PricePerPcUsd;
        var fee = AllocatedQuantityPcs * feePerPcAppliedUsd;

        GrossPayoutUsd = gross;
        FeePerPcAppliedUsd = feePerPcAppliedUsd;
        PlatformFeeUsd = fee;
        TotalPayoutUsd = Math.Max(gross - fee, 0);
    }

    public void SchedulePayout(DateTimeOffset scheduledAt) => PayoutScheduledAt = scheduledAt;

    public void MarkPaid(DateTimeOffset paidAt, string? method, string? reference)
    {
        PayoutPaidAt = paidAt;
        PayoutMethod = method;
        PayoutReference = reference;
    }

    public void BackOut(DateTimeOffset occurredAt, Guid? replacedByAllocationId = null)
    {
        Status = BidStatus.CancelledBackout;
        BackedOutAt = occurredAt;
        ReplacedByAllocationId = replacedByAllocationId;
    }
}
