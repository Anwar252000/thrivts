using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces issue_purchase_order — admin's manual fallback for a Confirmed deal whose
/// auto-issue trigger somehow didn't fire (the normal path never reaches this command at all).
/// Mirrors the RPC's guards exactly: deal must be Confirmed, and no PO may already exist for it.</summary>
public sealed record IssuePurchaseOrderCommand(Guid DealId, int DueDays = 3) : ICommand<ErrorOr<Guid>>;

public sealed class IssuePurchaseOrderCommandHandler : ICommandHandler<IssuePurchaseOrderCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public IssuePurchaseOrderCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(IssuePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can issue a purchase order.");

        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");
        if (deal.Status != DealStatus.Confirmed)
            return Error.Validation(description: $"A PO can only be issued on a confirmed deal (deal is {deal.Status}).");

        var alreadyExists = await _db.PurchaseOrders.AnyAsync(po => po.DealId == command.DealId, cancellationToken);
        if (alreadyExists)
            return Error.Validation(description: "A PO already exists for this deal.");

        var dueDays = Math.Max(command.DueDays, 1);
        var poNumber = await NextPoNumberAsync(cancellationToken);
        var po = new PurchaseOrder(command.DealId, poNumber, deal.TotalInvoiceUsd, _clock.UtcNow.AddDays(dueDays), _currentUser.UserId.Value);
        _db.PurchaseOrders.Add(po);

        _db.Notifications.Add(new Notification(
            deal.BuyerId, "Purchase Order issued",
            $"PO {poNumber} has been issued — please complete payment within {dueDays} days.",
            refType: "deal", refId: command.DealId));

        await _db.SaveChangesAsync(cancellationToken);
        return po.Id;
    }

    private async Task<string> NextPoNumberAsync(CancellationToken cancellationToken)
    {
        var year = _clock.UtcNow.Year;
        var prefix = $"PO-{year}-";
        var count = await _db.PurchaseOrders.CountAsync(po => po.PoNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
