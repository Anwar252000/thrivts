using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An influencer's commission on a referred buyer's Deal (the live schema's
/// influencer_commissions table). THRIVTS_FLOW_MATRIX.md §4.3 flags that the live system does
/// NOT yet call release/reverse on settle/cancel for this table (open bug #3) — Release/Reverse
/// here are that fix's new home once wired into AdvanceDealStatusCommandHandler.
/// </summary>
public class InfluencerCommission : BaseEntity, IAggregateRoot
{
    public Guid? InfluencerId { get; private set; }
    public Guid? BuyerId { get; private set; }
    public Guid? DealId { get; private set; }
    public decimal OrderValueUsd { get; private set; }
    public decimal Rate { get; private set; } = 0.05m;
    public decimal AmountUsd { get; private set; }
    public InfluencerCommissionStatus Status { get; private set; } = InfluencerCommissionStatus.Accrued;
    public DateTimeOffset? ReleasedAt { get; private set; }

    private InfluencerCommission()
    {
        // EF Core
    }

    public InfluencerCommission(decimal orderValueUsd, decimal rate, decimal amountUsd,
        Guid? influencerId = null, Guid? buyerId = null, Guid? dealId = null)
    {
        OrderValueUsd = orderValueUsd;
        Rate = rate;
        AmountUsd = amountUsd;
        InfluencerId = influencerId;
        BuyerId = buyerId;
        DealId = dealId;
    }

    /// <summary>Called when the underlying deal is marked Settled.</summary>
    public void Release(DateTimeOffset releasedAt)
    {
        Status = InfluencerCommissionStatus.Released;
        ReleasedAt = releasedAt;
    }

    /// <summary>Called when the underlying deal is Cancelled after this commission had accrued.</summary>
    public void Reverse() => Status = InfluencerCommissionStatus.Reversed;
}
