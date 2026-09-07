using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Sellers;

/// <summary>Replaces seller_respond_to_counter — accept/decline/counter the buyer's outstanding
/// counter on one of the seller's own bids (mirrors sellerRespond() in seller.html).</summary>
public sealed record RespondToBuyerCounterCommand(Guid BidId, string Action, decimal? NewPriceUsd, string? Note) : ICommand<ErrorOr<Success>>;

public sealed class RespondToBuyerCounterCommandHandler : ICommandHandler<RespondToBuyerCounterCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RespondToBuyerCounterCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(RespondToBuyerCounterCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var bid = await _db.SellerResponses.FirstOrDefaultAsync(sr => sr.Id == command.BidId, cancellationToken);
        if (bid is null)
            return Error.NotFound(description: $"Bid '{command.BidId}' was not found.");
        if (bid.SellerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only respond to counters on your own bids.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == bid.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{bid.RequirementId}' was not found.");

        var fee = bid.FeePerPcAppliedUsd ?? 0;
        Notification? notification;

        try
        {
            switch (command.Action)
            {
                case "accept":
                    bid.AcceptBuyerCounter();
                    notification = new Notification(requirement.BuyerId, "Seller accepted your counter",
                        $"You can now accept the bid at your price on {requirement.RequirementNumber}",
                        refType: "requirement", refId: bid.RequirementId);
                    break;
                case "decline":
                    bid.DeclineCounter();
                    notification = new Notification(requirement.BuyerId, "Seller declined your counter",
                        $"Your counter on {requirement.RequirementNumber} was declined",
                        refType: "requirement", refId: bid.RequirementId);
                    break;
                case "counter":
                    if (command.NewPriceUsd is null || command.NewPriceUsd <= 0)
                        return Error.Validation(description: "Enter a valid counter price.");
                    bid.SellerCounter(command.NewPriceUsd.Value, command.Note);
                    notification = new Notification(requirement.BuyerId, "Seller countered back",
                        $"New price: ${command.NewPriceUsd.Value + fee:F2}/pc on {requirement.RequirementNumber}",
                        refType: "requirement", refId: bid.RequirementId);
                    break;
                default:
                    return Error.Validation(description: $"Unknown action: {command.Action}");
            }
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        // Mirrors seller_respond_to_counter's perform push_notification(...) calls — never a table
        // trigger, so this has to be written explicitly here.
        _db.Notifications.Add(notification);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
