using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

/// <summary>Replaces the direct requirement_seller_offers.insert — admin pushes a direct offer to a seller.</summary>
public sealed record CreateOfferCommand(Guid RequirementId, Guid SellerId, decimal OfferPricePerPc) : ICommand<ErrorOr<Guid>>;

public sealed class CreateOfferCommandHandler : ICommandHandler<CreateOfferCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateOfferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateOfferCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can send a direct offer.");

        var offer = new RequirementSellerOffer(command.RequirementId, command.SellerId, command.OfferPricePerPc, _currentUser.UserId.Value);
        _db.RequirementSellerOffers.Add(offer);
        await _db.SaveChangesAsync(cancellationToken);

        return offer.Id;
    }
}
