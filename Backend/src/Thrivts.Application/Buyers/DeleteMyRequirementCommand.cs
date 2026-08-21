using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces buyer.html's deleteRequirement() — only while the requirement is still in an
/// early, uncommitted status and no deal has been created from it yet.</summary>
public sealed record DeleteMyRequirementCommand(Guid RequirementId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteMyRequirementCommandHandler : ICommandHandler<DeleteMyRequirementCommand, ErrorOr<Success>>
{
    private static readonly RequirementStatus[] DeletableStatuses =
        [RequirementStatus.PendingReview, RequirementStatus.Posted, RequirementStatus.Matching, RequirementStatus.ReadyToOrder, RequirementStatus.Cancelled];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteMyRequirementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteMyRequirementCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");
        if (requirement.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only delete your own requirements.");
        if (!DeletableStatuses.Contains(requirement.Status))
            return Error.Validation(description: "This requirement can no longer be deleted.");

        var hasDeal = await _db.Deals.AnyAsync(d => d.RequirementId == command.RequirementId, cancellationToken);
        if (hasDeal)
            return Error.Validation(description: "This requirement already has a deal and cannot be deleted.");

        var bids = _db.SellerResponses.Where(sr => sr.RequirementId == command.RequirementId);
        _db.SellerResponses.RemoveRange(bids);

        _db.Requirements.Remove(requirement);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
