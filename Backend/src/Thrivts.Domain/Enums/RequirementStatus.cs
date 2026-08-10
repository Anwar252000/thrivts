namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's requirement_status enum exactly (16 labels).</summary>
public enum RequirementStatus
{
    PendingReview,
    Posted,
    Matching,
    ReadyToOrder,
    Confirmed,
    AwaitingPayment,
    Paid,
    InFulfillment,
    SellersPaid,
    Dispatched,
    Delivered,
    Disputed,
    Settled,
    Cancelled,
    Expired,
    Stale
}
