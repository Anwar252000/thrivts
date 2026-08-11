using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

/// <summary>Replaces the direct buyers.update admin edit.</summary>
public sealed record UpdateBuyerCommand(
    Guid BuyerId,
    string CompanyName,
    string Country,
    string? City,
    string? Website,
    string? Instagram) : ICommand<ErrorOr<Success>>;

public sealed class UpdateBuyerCommandHandler : ICommandHandler<UpdateBuyerCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateBuyerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateBuyerCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can edit a buyer.");

        var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == command.BuyerId, cancellationToken);
        if (buyer is null)
            return Error.NotFound(description: $"Buyer '{command.BuyerId}' was not found.");

        buyer.UpdateProfile(command.CompanyName, command.Country, command.City, command.Website, command.Instagram);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
