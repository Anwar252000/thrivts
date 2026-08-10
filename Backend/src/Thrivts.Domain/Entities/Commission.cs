using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An agency's commission on a Deal it referred (the live schema's commissions table).
/// Accrues when the deal is Delivered, releases when Settled, reverses/cancels if Cancelled
/// before settlement (THRIVTS_FLOW_MATRIX.md §4.1/4.2). CommissionRate here is a PERCENTAGE
/// (matches Agency.CommissionRate), not a 0-1 fraction.
/// </summary>
public class Commission : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public Guid AgencyId { get; private set; }
    public decimal CommissionRate { get; private set; }
    public decimal GrossSpreadUsd { get; private set; }
    public decimal? SpreadUsd { get; private set; }
    public decimal? NetSettledSpreadUsd { get; private set; }
    public decimal CommissionAmountUsd { get; private set; }
    public decimal? FinalCommissionUsd { get; private set; }

    public CommissionStatus Status { get; private set; } = CommissionStatus.Pending;
    public DateTimeOffset ReleaseDueAt { get; private set; }
    public DateTimeOffset? AccruedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public Guid? ReleasedBy { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? PayoutMethod { get; private set; }
    public string? PayoutReference { get; private set; }

    private Commission()
    {
        // EF Core
    }

    public Commission(Guid dealId, Guid agencyId, decimal commissionRate, decimal grossSpreadUsd,
        decimal commissionAmountUsd, DateTimeOffset releaseDueAt)
    {
        DealId = dealId;
        AgencyId = agencyId;
        CommissionRate = commissionRate;
        GrossSpreadUsd = grossSpreadUsd;
        CommissionAmountUsd = commissionAmountUsd;
        ReleaseDueAt = releaseDueAt;
    }

    /// <summary>Called when the underlying deal is marked Delivered.</summary>
    public void Accrue(DateTimeOffset deliveredAt, DateTimeOffset releaseDueAt)
    {
        Status = CommissionStatus.Accrued;
        AccruedAt = deliveredAt;
        ReleaseDueAt = releaseDueAt;
    }

    /// <summary>Called when the underlying deal is marked Settled.</summary>
    public void Release(DateTimeOffset releasedAt, Guid releasedBy, decimal finalCommissionUsd, decimal netSettledSpreadUsd)
    {
        Status = CommissionStatus.Released;
        ReleasedAt = releasedAt;
        ReleasedBy = releasedBy;
        FinalCommissionUsd = finalCommissionUsd;
        NetSettledSpreadUsd = netSettledSpreadUsd;
    }

    public void MarkPaid(DateTimeOffset paidAt, string? method, string? reference)
    {
        PaidAt = paidAt;
        PayoutMethod = method;
        PayoutReference = reference;
    }

    /// <summary>Called when the underlying deal is Cancelled before settlement.</summary>
    public void Cancel() => Status = CommissionStatus.Cancelled;
}
