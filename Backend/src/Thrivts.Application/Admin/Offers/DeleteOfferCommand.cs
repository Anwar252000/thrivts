using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

public sealed record DeleteOfferCommand(Guid OfferId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteOfferCommandHandler : ICommandHandler<DeleteOfferCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteOfferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteOfferCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete an offer.");

        var offer = await _db.RequirementSellerOffers.FirstOrDefaultAsync(o => o.Id == command.OfferId, cancellationToken);
        if (offer is null)
            return Error.NotFound(description: $"Offer '{command.OfferId}' was not found.");

        _db.RequirementSellerOffers.Remove(offer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
