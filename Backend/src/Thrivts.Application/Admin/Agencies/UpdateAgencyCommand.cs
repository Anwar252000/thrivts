using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Agencies;

/// <summary>Replaces the direct agencies.update admin edit.</summary>
public sealed record UpdateAgencyCommand(Guid AgencyId, decimal? CommissionRate, bool? IsActive) : ICommand<ErrorOr<Success>>;

public sealed class UpdateAgencyCommandHandler : ICommandHandler<UpdateAgencyCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateAgencyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateAgencyCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can edit an agency.");

        var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == command.AgencyId, cancellationToken);
        if (agency is null)
            return Error.NotFound(description: $"Agency '{command.AgencyId}' was not found.");

        if (command.CommissionRate is not null)
            agency.SetCommissionRate(command.CommissionRate.Value);

        if (command.IsActive is true) agency.Reactivate();
        else if (command.IsActive is false) agency.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
