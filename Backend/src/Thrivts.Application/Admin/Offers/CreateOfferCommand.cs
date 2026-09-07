using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

/// <summary>Replaces the direct requirement_seller_offers.insert (admin.html's submitDirectOffer())
/// — admin pushes a direct offer to a seller, optionally for less than the full requirement
/// quantity (to split across sellers) and with a bounded expiry.</summary>
public sealed record CreateOfferCommand(
    Guid RequirementId, Guid SellerId, int QuantityPcs, decimal OfferPricePerPc, string? Notes, int ExpiresInDays)
    : ICommand<ErrorOr<Guid>>;

public sealed class CreateOfferCommandHandler : ICommandHandler<CreateOfferCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateOfferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateOfferCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can send a direct offer.");

        var requirement = await _db.Requirements.AsNoTracking().FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        var offerNumber = await NextOfferNumberAsync(cancellationToken);
        var expiresAt = _clock.UtcNow.AddDays(command.ExpiresInDays);

        var offer = new RequirementSellerOffer(
            offerNumber, command.RequirementId, command.SellerId, requirement.ItemName, command.QuantityPcs,
            requirement.Grade.ToString(), command.OfferPricePerPc, _currentUser.UserId.Value, command.Notes, expiresAt);

        _db.RequirementSellerOffers.Add(offer);
        await _db.SaveChangesAsync(cancellationToken);

        return offer.Id;
    }

    private async Task<string> NextOfferNumberAsync(CancellationToken cancellationToken)
    {
        var year = _clock.UtcNow.Year;
        var prefix = $"OFR-{year}-";
        var count = await _db.RequirementSellerOffers.CountAsync(o => o.OfferNumber != null && o.OfferNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
