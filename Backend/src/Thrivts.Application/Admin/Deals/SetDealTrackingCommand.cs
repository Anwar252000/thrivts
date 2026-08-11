using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

public sealed record SetDealTrackingCommand(Guid DealId, string? TrackingNumber, string? TrackingUrl, string? Courier) : ICommand<ErrorOr<Success>>;

public sealed class SetDealTrackingCommandHandler : ICommandHandler<SetDealTrackingCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetDealTrackingCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetDealTrackingCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can set a deal's tracking details.");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        deal.SetTrackingDetails(command.TrackingNumber, command.TrackingUrl, command.Courier);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
