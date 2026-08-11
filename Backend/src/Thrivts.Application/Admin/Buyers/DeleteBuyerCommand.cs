using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

/// <summary>Replaces admin_delete_buyer. Removes the buyer row only — the profile is left intact.</summary>
public sealed record DeleteBuyerCommand(Guid BuyerId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteBuyerCommandHandler : ICommandHandler<DeleteBuyerCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteBuyerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteBuyerCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete a buyer.");

        var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == command.BuyerId, cancellationToken);
        if (buyer is null)
            return Error.NotFound(description: $"Buyer '{command.BuyerId}' was not found.");

        _db.Buyers.Remove(buyer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
