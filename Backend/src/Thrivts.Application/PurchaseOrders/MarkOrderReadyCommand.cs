using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces seller_mark_order_ready — seller signals their own order is packed and ready
/// for pickup. Notifies every admin, matching the RPC's own loop over all admin profiles.</summary>
public sealed record MarkOrderReadyCommand(Guid DealId) : ICommand<ErrorOr<Success>>;

public sealed class MarkOrderReadyCommandHandler : ICommandHandler<MarkOrderReadyCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkOrderReadyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(MarkOrderReadyCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");
        if (deal.SellerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only mark your own orders ready.");

        try
        {
            deal.MarkOrderReady(_clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        var adminIds = await _db.Profiles.AsNoTracking().Where(p => p.Role == UserRole.Admin).Select(p => p.Id).ToListAsync(cancellationToken);
        foreach (var adminId in adminIds)
        {
            _db.Notifications.Add(new Notification(
                adminId, "Order ready for pickup",
                $"Seller marked order {deal.DealNumber} ready. Arrange pickup and dispatch.",
                refType: "deal", refId: command.DealId));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
