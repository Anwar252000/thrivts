using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

/// <summary>Replaces admin_block_buyer / admin_unblock_buyer (and the buyer-side approve/reject path).
/// Reason is only meaningful for Action == Reject.</summary>
public sealed record SetBuyerApprovalStatusCommand(Guid BuyerId, ProfileApprovalAction Action, string? Reason = null)
    : ICommand<ErrorOr<Success>>;
