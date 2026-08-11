using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>Replaces delete_seller. Removes the seller row only — the profile (login, audit
/// trail) is left intact, matching the live RPC's scope.</summary>
public sealed record DeleteSellerCommand(Guid SellerId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteSellerCommandHandler : ICommandHandler<DeleteSellerCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteSellerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteSellerCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete a seller.");

        var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == command.SellerId, cancellationToken);
        if (seller is null)
            return Error.NotFound(description: $"Seller '{command.SellerId}' was not found.");

        _db.Sellers.Remove(seller);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
