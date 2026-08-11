using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>Replaces toggleRequirementPublic() — shows/hides a requirement on the public landing feed.</summary>
public sealed record ToggleRequirementPublicCommand(Guid RequirementId, bool Public) : ICommand<ErrorOr<Success>>;

public sealed class ToggleRequirementPublicCommandHandler : ICommandHandler<ToggleRequirementPublicCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleRequirementPublicCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(ToggleRequirementPublicCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can change a requirement's public visibility.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        requirement.SetPublicDisplay(command.Public);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
