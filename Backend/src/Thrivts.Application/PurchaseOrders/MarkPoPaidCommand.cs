using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Replaces po_mark_paid — buyer uploads a payment receipt on their own still-open PO. The
/// receipt bytes arrive over our own API (multipart), never touching Supabase directly from the
/// browser; this handler pushes them to the private `receipts` Storage bucket server-side (see
/// ISupabaseStorageClient's own doc comment). Notifies every admin, matching the RPC's own loop.</summary>
public sealed record MarkPoPaidCommand(Guid DealId, byte[] ReceiptContent, string ReceiptFileName, string ContentType) : ICommand<ErrorOr<Success>>;

public sealed class MarkPoPaidCommandHandler : ICommandHandler<MarkPoPaidCommand, ErrorOr<Success>>
{
    private const string ReceiptsBucket = "receipts";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseStorageClient _storage;

    public MarkPoPaidCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, ISupabaseStorageClient storage)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _storage = storage;
    }

    public async ValueTask<ErrorOr<Success>> Handle(MarkPoPaidCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();
        if (command.ReceiptContent.Length == 0)
            return Error.Validation(description: "A receipt is required.");

        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null || deal.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only mark your own deals paid.");

        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.DealId == command.DealId, cancellationToken);
        if (po is null)
            return Error.NotFound(description: $"No purchase order exists for deal '{command.DealId}'.");

        var receiptPath = $"{command.DealId}/{Guid.NewGuid():N}-{command.ReceiptFileName}";

        try
        {
            await _storage.UploadAsync(ReceiptsBucket, receiptPath, command.ReceiptContent, command.ContentType, cancellationToken);
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Failure(description: ex.Message);
        }

        try
        {
            po.MarkPaymentSubmitted(receiptPath, _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        var adminIds = await _db.Profiles.AsNoTracking().Where(p => p.Role == UserRole.Admin).Select(p => p.Id).ToListAsync(cancellationToken);
        foreach (var adminId in adminIds)
        {
            _db.Notifications.Add(new Notification(
                adminId, "Payment submitted",
                $"A buyer submitted payment + receipt for PO {po.PoNumber} — please verify.",
                refType: "deal", refId: command.DealId));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
