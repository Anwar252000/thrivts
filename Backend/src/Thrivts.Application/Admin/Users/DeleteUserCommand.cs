using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Users;

/// <summary>
/// Removes a user's Profile and role-specific row (Buyer/Seller/Agency — Admin has none). Mirrors
/// DeleteBuyerCommand/DeleteSellerCommand's scope but also removes Profile itself, since this is a
/// full account deletion from a role-agnostic screen rather than "unlist this buyer but keep their
/// login". The Supabase Auth identity is intentionally left alone (no service-role key is wired
/// for identity deletion) — without a Profile row the account can no longer sign into any portal,
/// same end state admin.html's own delete flows leave behind.
/// </summary>
public sealed record DeleteUserCommand(Guid UserId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteUserCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete a user.");

        if (command.UserId == _currentUser.UserId)
            return Error.Validation(description: "You cannot delete your own account.");

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == command.UserId, cancellationToken);
        if (profile is null)
            return Error.NotFound(description: $"User '{command.UserId}' was not found.");

        switch (profile.Role)
        {
            case UserRole.Buyer:
                var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == command.UserId, cancellationToken);
                if (buyer is not null) _db.Buyers.Remove(buyer);
                break;
            case UserRole.Seller:
                var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == command.UserId, cancellationToken);
                if (seller is not null) _db.Sellers.Remove(seller);
                break;
            case UserRole.Agency:
                var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == command.UserId, cancellationToken);
                if (agency is not null) _db.Agencies.Remove(agency);
                break;
            case UserRole.Admin:
                break;
        }

        _db.Profiles.Remove(profile);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
