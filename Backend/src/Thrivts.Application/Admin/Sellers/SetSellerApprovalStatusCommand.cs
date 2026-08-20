using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>
/// Replaces approve_seller / reject_seller / block_seller / unblock_seller. [Authorize(Policy =
/// "AdminOnly")] at the controller is the first gate; the handler re-checks the role too.
/// Reason is only meaningful for Action == Reject. Tags is only meaningful for Action == Approve —
/// admin.html's approve_seller RPC always takes the category tags together with the approval,
/// because a seller approved with no tags sees zero requirements (Tags controls which category a
/// seller is matched against). Empty/omitted is allowed (mirrors the live "approve anyway" confirm).
/// </summary>
public sealed record SetSellerApprovalStatusCommand(Guid SellerId, ProfileApprovalAction Action, string? Reason = null, string[]? Tags = null)
    : ICommand<ErrorOr<Success>>;
