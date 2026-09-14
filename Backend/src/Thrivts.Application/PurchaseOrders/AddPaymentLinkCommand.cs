using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces po_add_payment_link — admin attaches a payment link to a still-open PO.</summary>
public sealed record AddPaymentLinkCommand(Guid DealId, string Link) : ICommand<ErrorOr<Success>>;

public sealed class AddPaymentLinkCommandHandler : ICommandHandler<AddPaymentLinkCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddPaymentLinkCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(AddPaymentLinkCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can attach a payment link.");
        if (string.IsNullOrWhiteSpace(command.Link))
            return Error.Validation(description: "Link required.");

        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.DealId == command.DealId, cancellationToken);
        if (po is null)
            return Error.NotFound(description: $"No purchase order exists for deal '{command.DealId}'.");

        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        try
        {
            po.AddPaymentLink(command.Link.Trim());
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        _db.Notifications.Add(new Notification(
            deal.BuyerId, "Payment link ready",
            $"Your payment link for PO {po.PoNumber} is ready.",
            refType: "deal", refId: command.DealId));

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
