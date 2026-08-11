using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

/// <summary>
/// Replaces admin_accept_offer. NOTE: matches the live RPC exactly — accepting an offer does NOT
/// itself create a deal; admin still runs CreateDealFromMatchCommand separately
/// (THRIVTS_FLOW_MATRIX.md §5).
/// </summary>
public sealed record AdminAcceptOfferCommand(Guid OfferId) : ICommand<ErrorOr<Success>>;

public sealed class AdminAcceptOfferCommandHandler : ICommandHandler<AdminAcceptOfferCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AdminAcceptOfferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(AdminAcceptOfferCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can accept an offer.");

        var offer = await _db.RequirementSellerOffers.FirstOrDefaultAsync(o => o.Id == command.OfferId, cancellationToken);
        if (offer is null)
            return Error.NotFound(description: $"Offer '{command.OfferId}' was not found.");

        offer.Accept(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
