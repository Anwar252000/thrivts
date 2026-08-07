using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

/// <summary>Replaces admin_block_buyer / admin_unblock_buyer (and the buyer-side approve/reject path).</summary>
public sealed record SetBuyerApprovalStatusCommand(Guid BuyerId, ProfileApprovalAction Action)
    : ICommand<ErrorOr<Success>>;
