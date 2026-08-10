namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors the live schema's deal_status enum exactly (14 labels). The real lifecycle is:
/// Draft -> Confirmed -> AwaitingPayment -> Paid -> InFulfillment -> Dispatched -> Delivered ->
/// Settled, with Cancelled/Disputed off-ramps (see DealStateMachine).
/// Pending/Accrued/Released/Reversed are enum labels the live DB defines but that read as
/// commission-lifecycle leftovers, not real deal states — kept here only so a column read can
/// never fail to deserialize; DealStateMachine does not route through them.
/// </summary>
public enum DealStatus
{
    Draft,
    Confirmed,
    AwaitingPayment,
    Paid,
    InFulfillment,
    Dispatched,
    Delivered,
    Settled,
    Cancelled,
    Disputed,
    Pending,
    Accrued,
    Released,
    Reversed
}
