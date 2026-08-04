namespace Thrivts.Domain.Enums;

/// <summary>
/// match -> confirmed -> paid -> in_fulfillment -> dispatched -> delivered -> settled
/// Off-ramps: cancelled, disputed.
/// </summary>
public enum DealStatus
{
    Match,
    Confirmed,
    Paid,
    InFulfillment,
    Dispatched,
    Delivered,
    Settled,
    Cancelled,
    Disputed
}
