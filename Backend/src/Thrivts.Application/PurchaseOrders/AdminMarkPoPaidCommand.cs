using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces admin_mark_po_paid — admin's one-step "mark paid" shortcut, valid from either
/// Issued or PaymentSubmitted (skips the buyer-receipt flow entirely if used). See
/// PurchaseOrder.MarkVerified's own doc comment for why this shares its domain method with
/// VerifyPurchaseOrderCommand.</summary>
public sealed record AdminMarkPoPaidCommand(Guid DealId) : ICommand<ErrorOr<Success>>;

public sealed class AdminMarkPoPaidCommandHandler : ICommandHandler<AdminMarkPoPaidCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AdminMarkPoPaidCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(AdminMarkPoPaidCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can mark a purchase order paid.");

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

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
