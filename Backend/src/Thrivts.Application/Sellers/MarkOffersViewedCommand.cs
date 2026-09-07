using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Sellers;

/// <summary>Replaces loadOffers()'s bulk `.update({ viewed_at, status: 'viewed' }).in('id', unviewed)`
/// side effect — called once the seller's offers list has loaded, for whichever of those offers are
/// still Sent. Silently no-ops on ids that don't belong to the caller or aren't Sent.</summary>
public sealed record MarkOffersViewedCommand(Guid[] OfferIds) : ICommand<ErrorOr<Success>>;

public sealed class MarkOffersViewedCommandHandler : ICommandHandler<MarkOffersViewedCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkOffersViewedCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(MarkOffersViewedCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        if (command.OfferIds.Length == 0)
            return Result.Success;

        var sellerId = _currentUser.UserId.Value;
        var offers = await _db.RequirementSellerOffers
            .Where(o => command.OfferIds.Contains(o.Id) && o.SellerId == sellerId)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        foreach (var offer in offers)
            offer.MarkViewed(now);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
