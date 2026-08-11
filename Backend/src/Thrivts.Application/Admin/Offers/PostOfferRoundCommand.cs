using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

/// <summary>
/// Replaces post_offer_round. Kind is 'counter' (moves the price and the offer status) or
/// 'message' (a note only, price left untouched) — matches the two admin.html call sites.
/// </summary>
public sealed record PostOfferRoundCommand(Guid OfferId, string Kind, decimal? PricePerPcUsd, string? Notes) : ICommand<ErrorOr<Success>>;

public sealed class PostOfferRoundCommandHandler : ICommandHandler<PostOfferRoundCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public PostOfferRoundCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(PostOfferRoundCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can post an offer round.");

        var offer = await _db.RequirementSellerOffers.FirstOrDefaultAsync(o => o.Id == command.OfferId, cancellationToken);
        if (offer is null)
            return Error.NotFound(description: $"Offer '{command.OfferId}' was not found.");

        if (command.Kind == "counter")
        {
            if (command.PricePerPcUsd is null)
                return Error.Validation(description: "PricePerPcUsd is required for a counter round.");

            offer.PostRound(command.PricePerPcUsd.Value, _clock.UtcNow);
        }

        var round = new OfferRound(command.OfferId, NegotiationActor.Admin, command.Kind, command.PricePerPcUsd, command.Notes, _currentUser.UserId);
        _db.OfferRounds.Add(round);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
