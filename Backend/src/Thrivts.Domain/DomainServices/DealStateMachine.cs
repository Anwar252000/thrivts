using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.DomainServices;

/// <summary>
/// match -> confirmed -> paid -> in_fulfillment -> dispatched -> delivered -> settled
/// Off-ramps: cancelled (from any pre-settled state), disputed (from any post-confirmed state).
/// Replaces the old Postgres advance_deal_status RPC.
/// </summary>
public static class DealStateMachine
{
    private static readonly Dictionary<DealStatus, DealStatus[]> AllowedTransitions = new()
    {
        [DealStatus.Match] = [DealStatus.Confirmed, DealStatus.Cancelled],
        [DealStatus.Confirmed] = [DealStatus.Paid, DealStatus.Cancelled, DealStatus.Disputed],
        [DealStatus.Paid] = [DealStatus.InFulfillment, DealStatus.Cancelled, DealStatus.Disputed],
        [DealStatus.InFulfillment] = [DealStatus.Dispatched, DealStatus.Cancelled, DealStatus.Disputed],
        [DealStatus.Dispatched] = [DealStatus.Delivered, DealStatus.Disputed],
        [DealStatus.Delivered] = [DealStatus.Settled, DealStatus.Disputed],
        [DealStatus.Settled] = [],
        [DealStatus.Cancelled] = [],
        [DealStatus.Disputed] = [DealStatus.Confirmed, DealStatus.Paid, DealStatus.InFulfillment, DealStatus.Dispatched, DealStatus.Delivered, DealStatus.Cancelled]
    };

    public static bool CanTransition(DealStatus from, DealStatus to) =>
        AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to);

    public static void EnsureValidTransition(DealStatus from, DealStatus to)
    {
        if (!CanTransition(from, to))
            throw new DomainException($"Cannot transition a deal from '{from}' to '{to}'.");
    }
}
