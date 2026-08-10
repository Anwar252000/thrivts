using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

public sealed class SetBuyerApprovalStatusCommandHandler
    : ICommandHandler<SetBuyerApprovalStatusCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetBuyerApprovalStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetBuyerApprovalStatusCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can change a buyer's approval status.");

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == command.BuyerId, cancellationToken);
        if (profile is null)
            return Error.NotFound(description: $"Buyer '{command.BuyerId}' was not found.");
        if (profile.Role != UserRole.Buyer)
            return Error.Validation(description: $"Profile '{command.BuyerId}' is not a buyer.");

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
