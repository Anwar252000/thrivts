using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>Replaces the direct requirements.update admin edit.</summary>
public sealed record UpdateRequirementCommand(
    Guid RequirementId,
    string ItemName,
    int QuantityPcs,
    GradeType Grade,
    string DestinationCountry,
    decimal BuyerTargetPriceUsd,
    string? AdminNotes) : ICommand<ErrorOr<Success>>;

public sealed class UpdateRequirementCommandHandler : ICommandHandler<UpdateRequirementCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateRequirementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateRequirementCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can edit a requirement.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        requirement.UpdateDetails(command.ItemName, command.QuantityPcs, command.Grade, command.DestinationCountry,
            command.BuyerTargetPriceUsd, command.AdminNotes);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
