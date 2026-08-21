using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces buyer_counter_bid — CounterBuyerPriceUsd is the fee-inclusive (buyer-facing)
/// price; SellerResponse.BuyerCounter() converts it back to the seller-side number internally.</summary>
public sealed record BuyerCounterBidCommand(Guid BidId, decimal CounterBuyerPriceUsd, string? Note) : ICommand<ErrorOr<Success>>;

public sealed class BuyerCounterBidCommandHandler : ICommandHandler<BuyerCounterBidCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BuyerCounterBidCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(BuyerCounterBidCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var bid = await _db.SellerResponses.FirstOrDefaultAsync(sr => sr.Id == command.BidId, cancellationToken);
        if (bid is null)
            return Error.NotFound(description: $"Bid '{command.BidId}' was not found.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == bid.RequirementId, cancellationToken);
        if (requirement is null || requirement.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only counter bids on your own requirements.");

        try
        {
            bid.BuyerCounter(command.CounterBuyerPriceUsd, command.Note);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
