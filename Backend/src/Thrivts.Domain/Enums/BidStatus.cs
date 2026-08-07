namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors seller_responses.status (the live schema's seller_response_status enum).
/// Distinct from NegotiationState, which tracks the back-and-forth on the currently-open bid.
/// </summary>
public enum BidStatus
{
    Pending,
    Accepted,
    Declined,
    Rejected,
    Withdrawn,
    Expired,
    Cancelled
}
