using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>
/// Replaces approve_seller / reject_seller / block_seller / unblock_seller. [Authorize(Policy =
/// "AdminOnly")] at the controller is the first gate; the handler re-checks the role too.
/// </summary>
public sealed record SetSellerApprovalStatusCommand(Guid SellerId, ProfileApprovalAction Action)
    : ICommand<ErrorOr<Success>>;
