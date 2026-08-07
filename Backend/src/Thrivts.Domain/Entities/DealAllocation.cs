using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One seller's committed slice of a Deal (the live schema's deal_allocations table — today
/// always 1:1 with a Deal since multi-seller fills create separate deals, but modelled as its
/// own aggregate to match the source table and STEP2's per-allocation fee snapshot).
/// </summary>
public class DealAllocation : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid? SellerResponseId { get; private set; }
    public int AllocatedQuantityPcs { get; private set; }
    public decimal PricePerPcUsd { get; private set; }

    public decimal? GrossPayoutUsd { get; private set; }
    public decimal? FeePerPcAppliedUsd { get; private set; }
    public decimal? PlatformFeeUsd { get; private set; }
    public decimal? TotalPayoutUsd { get; private set; }
    public DateTimeOffset? PayoutPaidAt { get; private set; }

    private DealAllocation()
    {
        // EF Core
    }

    public DealAllocation(Guid dealId, Guid sellerId, int allocatedQuantityPcs, decimal pricePerPcUsd,
        Guid? sellerResponseId = null)
    {
        DealId = dealId;
        SellerId = sellerId;
        SellerResponseId = sellerResponseId;
        AllocatedQuantityPcs = allocatedQuantityPcs;
        PricePerPcUsd = pricePerPcUsd;
    }

    /// <summary>
    /// Freezes the platform fee onto this allocation (replaces apply_allocation_fee). Net payout
    /// = gross - fee, matching the live schema's total_payout_usd semantics.
    /// </summary>
    public void FreezeFee(decimal feePerPcAppliedUsd)
    {
        var gross = AllocatedQuantityPcs * PricePerPcUsd;
        var fee = AllocatedQuantityPcs * feePerPcAppliedUsd;

        GrossPayoutUsd = gross;
        FeePerPcAppliedUsd = feePerPcAppliedUsd;
        PlatformFeeUsd = fee;
        TotalPayoutUsd = Math.Max(gross - fee, 0);
    }

    public void MarkPaid(DateTimeOffset paidAt) => PayoutPaidAt = paidAt;
}
