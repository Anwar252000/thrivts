namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's commission_status enum exactly.</summary>
public enum CommissionStatus
{
    Pending,
    ReadyToRelease,
    Released,
    Cancelled,
    Accrued,
    Reversed
}
