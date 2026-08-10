namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors the live schema's seller_response_status enum exactly. Accrued/Released/Reversed read
/// as commission-lifecycle leftovers bled into this enum type, not real bid states — kept for
/// safe round-tripping only (see DealStatus for the same pattern).
/// </summary>
public enum BidStatus
{
    Pending,
    Accepted,
    Rejected,
    CancelledBackout,
    Accrued,
    Released,
    Reversed
}
