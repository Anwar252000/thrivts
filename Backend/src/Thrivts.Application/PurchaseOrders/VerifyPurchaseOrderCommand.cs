using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces verify_purchase_order — admin verifies a buyer's submitted receipt. Notifies
/// the buyer, matching the RPC's own push_notification call (an update path, not the trigger-covered
/// insert path — see BuyerCounterBidCommand's identical reasoning for writing Notification rows
/// explicitly here).</summary>
public sealed record VerifyPurchaseOrderCommand(Guid DealId) : ICommand<ErrorOr<Success>>;

public sealed class VerifyPurchaseOrderCommandHandler : ICommandHandler<VerifyPurchaseOrderCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public VerifyPurchaseOrderCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(VerifyPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can verify a purchase order.");

        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.DealId == command.DealId, cancellationToken);
        if (po is null)
            return Error.NotFound(description: $"No purchase order exists for deal '{command.DealId}'.");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        var now = _clock.UtcNow;
        try
        {
            po.MarkVerified(_currentUser.UserId, now);
            deal.AdvanceTo(DealStatus.Paid, now);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        _db.Notifications.Add(new Notification(
            deal.BuyerId, "Payment verified",
            $"Your payment for PO {po.PoNumber} is verified — your order is now moving to fulfillment.",
            refType: "deal", refId: command.DealId));

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
