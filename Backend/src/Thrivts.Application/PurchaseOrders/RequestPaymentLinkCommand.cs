using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces po_request_payment_link — buyer asks admin for a payment link on their own
/// still-open PO. Notifies every admin, matching the RPC's own loop over all admin profiles.</summary>
public sealed record RequestPaymentLinkCommand(Guid DealId) : ICommand<ErrorOr<Success>>;

public sealed class RequestPaymentLinkCommandHandler : ICommandHandler<RequestPaymentLinkCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public RequestPaymentLinkCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(RequestPaymentLinkCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null || deal.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only request a payment link on your own deals.");

        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.DealId == command.DealId, cancellationToken);
        if (po is null)
            return Error.NotFound(description: $"No purchase order exists for deal '{command.DealId}'.");

        try
        {
            po.RequestPaymentLink(_clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        var adminIds = await _db.Profiles.AsNoTracking().Where(p => p.Role == UserRole.Admin).Select(p => p.Id).ToListAsync(cancellationToken);
        foreach (var adminId in adminIds)
        {
            _db.Notifications.Add(new Notification(
                adminId, "Payment link requested",
                $"A buyer requested a payment link for PO {po.PoNumber}.",
                refType: "deal", refId: command.DealId));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
