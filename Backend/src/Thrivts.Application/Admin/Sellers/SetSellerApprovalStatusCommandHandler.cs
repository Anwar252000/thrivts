using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

public sealed class SetSellerApprovalStatusCommandHandler
    : ICommandHandler<SetSellerApprovalStatusCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetSellerApprovalStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetSellerApprovalStatusCommand command, CancellationToken cancellationToken)
    {
        // Controller-level [Authorize(Policy = "AdminOnly")] is the first gate; re-check here too —
        // never trust that only the admin UI can reach this handler.
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can change a seller's approval status.");

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == command.SellerId, cancellationToken);
        if (profile is null)
            return Error.NotFound(description: $"Seller '{command.SellerId}' was not found.");
        if (profile.Role != UserRole.Seller)
            return Error.Validation(description: $"Profile '{command.SellerId}' is not a seller.");

        switch (command.Action)
        {
            case ProfileApprovalAction.Approve:
                profile.Approve(_currentUser.UserId.Value, _clock.UtcNow);
                break;
            case ProfileApprovalAction.Reject:
                profile.Reject(command.Reason ?? "No reason given");
                break;
            case ProfileApprovalAction.Block:
                profile.Block();
                break;
            case ProfileApprovalAction.Unblock:
                profile.Unblock();
                break;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
