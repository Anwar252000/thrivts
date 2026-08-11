using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

/// <summary>Replaces admin_decline_offer.</summary>
public sealed record AdminDeclineOfferCommand(Guid OfferId, string Reason) : ICommand<ErrorOr<Success>>;

public sealed class AdminDeclineOfferCommandHandler : ICommandHandler<AdminDeclineOfferCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AdminDeclineOfferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(AdminDeclineOfferCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can decline an offer.");

        var offer = await _db.RequirementSellerOffers.FirstOrDefaultAsync(o => o.Id == command.OfferId, cancellationToken);
        if (offer is null)
            return Error.NotFound(description: $"Offer '{command.OfferId}' was not found.");

        offer.Decline(_clock.UtcNow);

        var round = new OfferRound(command.OfferId, NegotiationActor.Admin, "decline", null, command.Reason, _currentUser.UserId);
        _db.OfferRounds.Add(round);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
