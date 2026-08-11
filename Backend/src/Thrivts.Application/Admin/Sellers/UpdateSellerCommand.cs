using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>Replaces the direct sellers.update admin edit (tier, tags, contact details). Any
/// field left null is not changed.</summary>
public sealed record UpdateSellerCommand(
    Guid SellerId,
    SellerTier? Tier,
    string[]? Tags,
    string? Phone,
    string? WhatsApp,
    string? ReferenceContact,
    string? TierNotes) : ICommand<ErrorOr<Success>>;

public sealed class UpdateSellerCommandHandler : ICommandHandler<UpdateSellerCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public UpdateSellerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateSellerCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can edit a seller.");

        var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == command.SellerId, cancellationToken);
        if (seller is null)
            return Error.NotFound(description: $"Seller '{command.SellerId}' was not found.");

        if (command.Tier is not null)
            seller.SetTier(command.Tier.Value, _currentUser.UserId.Value, _clock.UtcNow, command.TierNotes);

        if (command.Tags is not null)
            seller.SetTags(command.Tags);

        if (command.Phone is not null || command.WhatsApp is not null || command.ReferenceContact is not null)
            seller.UpdateContactDetails(command.Phone, command.WhatsApp, command.ReferenceContact);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
