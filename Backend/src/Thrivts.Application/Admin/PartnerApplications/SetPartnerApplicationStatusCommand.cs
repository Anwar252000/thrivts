using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.PartnerApplications;

/// <summary>
/// Replaces admin_set_partner_application_status. Approving requires an already-created
/// Influencer (admin runs CreateInfluencerCommand first, then links it here) — matches the live
/// two-step flow (admin_create_influencer -> admin_set_partner_application_status).
/// </summary>
public sealed record SetPartnerApplicationStatusCommand(Guid ApplicationId, bool Approve, Guid? InfluencerId) : ICommand<ErrorOr<Success>>;

public sealed class SetPartnerApplicationStatusCommandHandler
    : ICommandHandler<SetPartnerApplicationStatusCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetPartnerApplicationStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetPartnerApplicationStatusCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can review partner applications.");

        var application = await _db.PartnerApplications.FirstOrDefaultAsync(a => a.Id == command.ApplicationId, cancellationToken);
        if (application is null)
            return Error.NotFound(description: $"Partner application '{command.ApplicationId}' was not found.");

        if (command.Approve)
        {
            if (command.InfluencerId is null)
                return Error.Validation(description: "InfluencerId is required to approve — create the influencer first.");

            application.Approve(command.InfluencerId.Value, _clock.UtcNow);
        }
        else
        {
            application.Reject(_clock.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
