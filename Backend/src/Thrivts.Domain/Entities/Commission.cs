using Thrivts.Domain.Common;
using Thrivts.Domain.DomainServices;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An agency's commission on a Deal it referred (the live schema's commissions table).
/// Accrues when the deal is Delivered (release due = delivered + CommissionCalculator.CommissionReleaseDelay),
/// releases when the deal is Settled, reverses/cancels if the deal is Cancelled (see
/// THRIVTS_FLOW_MATRIX.md §4.1/4.2).
/// </summary>
public class Commission : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public Guid AgencyId { get; private set; }
    public decimal AmountUsd { get; private set; }
    public CommissionStatus Status { get; private set; } = CommissionStatus.Pending;
    public DateTimeOffset? ReleaseDueAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    private Commission()
    {
        // EF Core
    }

    public Commission(Guid dealId, Guid agencyId, decimal amountUsd)
    {
        DealId = dealId;
        AgencyId = agencyId;
        AmountUsd = amountUsd;
    }

    /// <summary>Called when the underlying deal is marked Delivered.</summary>
    public void Accrue(DateTimeOffset deliveredAt)
    {
        Status = CommissionStatus.ReadyToRelease;
        ReleaseDueAt = deliveredAt + CommissionCalculator.CommissionReleaseDelay;
    }

    /// <summary>Called when the underlying deal is marked Settled.</summary>
    public void Release(DateTimeOffset releasedAt)
    {
        Status = CommissionStatus.Released;
        ReleasedAt = releasedAt;
    }

    /// <summary>Called when the underlying deal is Cancelled before settlement.</summary>
    public void Cancel() => Status = CommissionStatus.Cancelled;
}
