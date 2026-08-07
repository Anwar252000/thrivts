namespace Thrivts.Domain.Enums;

/// <summary>
/// posted -> matching -> ready_to_order -> in_fulfillment -> settled, with cancelled as an off-ramp.
/// Mirrors the requirements.status values driven by buyer_accept_bid / finalize_bid_to_deal /
/// the cancel cascade in the live schema.
/// </summary>
public enum RequirementStatus
{
    Posted,
    Matching,
    ReadyToOrder,
    InFulfillment,
    Settled,
    Cancelled
}
