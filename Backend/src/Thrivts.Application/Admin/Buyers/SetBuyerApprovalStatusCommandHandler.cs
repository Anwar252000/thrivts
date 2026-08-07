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

    public SetBuyerApprovalStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetBuyerApprovalStatusCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can change a buyer's approval status.");

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == command.BuyerId, cancellationToken);
        if (profile is null)
            return Error.NotFound(description: $"Buyer '{command.BuyerId}' was not found.");
        if (profile.Role != UserRole.Buyer)
            return Error.Validation(description: $"Profile '{command.BuyerId}' is not a buyer.");

        switch (command.Action)
        {
            case ProfileApprovalAction.Approve:
                profile.Approve();
                break;
            case ProfileApprovalAction.Reject:
                profile.Reject();
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
