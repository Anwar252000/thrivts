using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces buyer_accept_bid — buyer accepts a bid on their own requirement directly,
/// which creates the Deal immediately (enforces remaining-quantity limits; a buyer can accept
/// multiple bids to fill one requirement). The money math lives in AcceptBidService, shared with
/// AdminAcceptSellerResponseCommand.</summary>
public sealed record BuyerAcceptBidCommand(Guid BidId) : ICommand<ErrorOr<Guid>>;

public sealed class BuyerAcceptBidCommandHandler : ICommandHandler<BuyerAcceptBidCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly AcceptBidService _acceptBidService;

    public BuyerAcceptBidCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, AcceptBidService acceptBidService)
    {
        _db = db;
        _currentUser = currentUser;
        _acceptBidService = acceptBidService;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(BuyerAcceptBidCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var bid = await _db.SellerResponses.AsNoTracking().FirstOrDefaultAsync(sr => sr.Id == command.BidId, cancellationToken);
        if (bid is null)
            return Error.NotFound(description: $"Bid '{command.BidId}' was not found.");

        var requirement = await _db.Requirements.AsNoTracking().FirstOrDefaultAsync(r => r.Id == bid.RequirementId, cancellationToken);
        if (requirement is null || requirement.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only accept bids on your own requirements.");

        return await _acceptBidService.AcceptAsync(command.BidId, _currentUser.UserId.Value, cancellationToken);
    }
}
