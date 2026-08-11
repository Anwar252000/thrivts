using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>Replaces pushRequirementLive() — moves a pending_review requirement to Posted and
/// makes it visible to matching sellers.</summary>
public sealed record PostRequirementLiveCommand(Guid RequirementId) : ICommand<ErrorOr<Success>>;

public sealed class PostRequirementLiveCommandHandler : ICommandHandler<PostRequirementLiveCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public PostRequirementLiveCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(PostRequirementLiveCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can post a requirement live.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        requirement.Post(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
