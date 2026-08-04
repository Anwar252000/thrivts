using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Admin.Deals;

public sealed class AdvanceDealStatusCommandHandler : ICommandHandler<AdvanceDealStatusCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AdvanceDealStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(AdvanceDealStatusCommand command, CancellationToken cancellationToken)
    {
        // Controller-level [Authorize(Policy = "AdminOnly")] is the first gate; re-check here too —
        // never trust that only the admin UI can reach this handler.
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can advance a deal's status.");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        try
        {
            deal.AdvanceTo(command.NewStatus, _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
