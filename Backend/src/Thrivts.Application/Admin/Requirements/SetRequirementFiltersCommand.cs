using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>Replaces editRequirementTagsAndTier() — controls seller-side visibility (min tier +
/// optional tag restriction), separate from UpdateRequirementCommand's listing-content fields.</summary>
public sealed record SetRequirementFiltersCommand(Guid RequirementId, SellerTier MinSellerTier, string[]? RestrictedToTags)
    : ICommand<ErrorOr<Success>>;

public sealed class SetRequirementFiltersCommandHandler : ICommandHandler<SetRequirementFiltersCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetRequirementFiltersCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetRequirementFiltersCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can change a requirement's matching filters.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        requirement.SetMatchingFilters(command.MinSellerTier, command.RestrictedToTags);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
