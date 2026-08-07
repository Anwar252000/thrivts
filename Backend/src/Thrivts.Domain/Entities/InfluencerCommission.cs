using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An influencer's commission on a referred buyer's Deal (the live schema's
/// influencer_commissions table). THRIVTS_FLOW_MATRIX.md §4.3 flags that the live system does
/// NOT yet call release/reverse on settle/cancel for this table (open bug #3) — the Release and
/// Reverse methods here are that fix's new home once wired into AdvanceDealStatusCommandHandler.
/// </summary>
public class InfluencerCommission : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public Guid InfluencerId { get; private set; }
    public Guid BuyerId { get; private set; }
    public decimal AmountUsd { get; private set; }
    public InfluencerCommissionStatus Status { get; private set; } = InfluencerCommissionStatus.Accrued;
    public DateTimeOffset? ReleasedAt { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }

    private InfluencerCommission()
    {
        // EF Core
    }

    public InfluencerCommission(Guid dealId, Guid influencerId, Guid buyerId, decimal amountUsd)
    {
        DealId = dealId;
        InfluencerId = influencerId;
        BuyerId = buyerId;
        AmountUsd = amountUsd;
    }

    /// <summary>Called when the underlying deal is marked Settled.</summary>
    public void Release(DateTimeOffset releasedAt)
    {
        Status = InfluencerCommissionStatus.Released;
        ReleasedAt = releasedAt;
    }

    /// <summary>Called when the underlying deal is Cancelled after this commission had accrued.</summary>
    public void Reverse(DateTimeOffset reversedAt)
    {
        Status = InfluencerCommissionStatus.Reversed;
        ReversedAt = reversedAt;
    }
}
