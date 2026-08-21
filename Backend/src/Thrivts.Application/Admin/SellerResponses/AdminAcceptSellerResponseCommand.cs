using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.SellerResponses;

/// <summary>
/// Replaces accept_seller_quote — admin directly accepts a seller's bid on the buyer's behalf.
/// The actual deal-creation math lives in AcceptBidService, shared with BuyerAcceptBidCommand.
/// </summary>
public sealed record AdminAcceptSellerResponseCommand(Guid SellerResponseId) : ICommand<ErrorOr<Guid>>;

public sealed class AdminAcceptSellerResponseCommandHandler
    : ICommandHandler<AdminAcceptSellerResponseCommand, ErrorOr<Guid>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly AcceptBidService _acceptBidService;

    public AdminAcceptSellerResponseCommandHandler(ICurrentUserService currentUser, AcceptBidService acceptBidService)
    {
        _currentUser = currentUser;
        _acceptBidService = acceptBidService;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(AdminAcceptSellerResponseCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can accept a seller quote directly.");

        return await _acceptBidService.AcceptAsync(command.SellerResponseId, _currentUser.UserId.Value, cancellationToken);
    }
}
