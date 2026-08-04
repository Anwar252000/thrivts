using Thrivts.Domain.Common;
using Thrivts.Domain.DomainServices;
using Thrivts.Domain.Enums;
using Thrivts.Domain.ValueObjects;

namespace Thrivts.Domain.Entities;

public class Deal : BaseEntity, IAggregateRoot
{
    public string DealNumber { get; private set; } = default!;
    public Guid BuyerId { get; private set; }
    public Guid? AgencyId { get; private set; }
    public Guid RequirementId { get; private set; }
    public DealStatus Status { get; private set; } = DealStatus.Match;
    public Money TotalInvoice { get; private set; }
    public Money TotalSpread { get; private set; }
    public int TotalQuantityPcs { get; private set; }

    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private Deal()
    {
        // EF Core
    }

    public Deal(string dealNumber, Guid buyerId, Guid requirementId, Money totalInvoice, Money totalSpread, int totalQuantityPcs, Guid? agencyId = null)
    {
        DealNumber = dealNumber;
        BuyerId = buyerId;
        RequirementId = requirementId;
        TotalInvoice = totalInvoice;
        TotalSpread = totalSpread;
        TotalQuantityPcs = totalQuantityPcs;
        AgencyId = agencyId;
    }

    /// <summary>
    /// Replaces the old advance_deal_status RPC. Invalid transitions throw rather than silently no-op.
    /// </summary>
    public void AdvanceTo(DealStatus next, DateTimeOffset occurredAt)
    {
        DealStateMachine.EnsureValidTransition(Status, next);

        Status = next;
        switch (next)
        {
            case DealStatus.Paid:
                PaidAt = occurredAt;
                break;
            case DealStatus.Dispatched:
                DispatchedAt = occurredAt;
                break;
            case DealStatus.Delivered:
                DeliveredAt = occurredAt;
                break;
            case DealStatus.Settled:
                SettledAt = occurredAt;
                break;
        }
    }

    public void Cancel(string reason)
    {
        DealStateMachine.EnsureValidTransition(Status, DealStatus.Cancelled);
        Status = DealStatus.Cancelled;
        CancellationReason = reason;
    }
}
