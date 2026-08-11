using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Disputes;

public sealed record BeginDisputeInvestigationCommand(Guid DisputeId) : ICommand<ErrorOr<Success>>;

public sealed class BeginDisputeInvestigationCommandHandler : ICommandHandler<BeginDisputeInvestigationCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BeginDisputeInvestigationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(BeginDisputeInvestigationCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can begin a dispute investigation.");

        var dispute = await _db.Disputes.FirstOrDefaultAsync(d => d.Id == command.DisputeId, cancellationToken);
        if (dispute is null)
            return Error.NotFound(description: $"Dispute '{command.DisputeId}' was not found.");

        dispute.BeginInvestigation();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
