using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Mirrors admin.html's db.storage.from('receipts').createSignedUrl(po.receipt_url, 3600)
/// call — generates a time-limited link to a buyer-uploaded receipt in the private `receipts`
/// bucket so an admin can actually look at the payment proof. Admin-only (gated by the controller's
/// [Authorize(Policy="AdminOnly")], same as the rest of AdminPurchaseOrdersController).</summary>
public sealed record GetReceiptSignedUrlQuery(Guid DealId) : IQuery<ErrorOr<string>>;

public sealed class GetReceiptSignedUrlQueryHandler : IQueryHandler<GetReceiptSignedUrlQuery, ErrorOr<string>>
{
    private const string ReceiptsBucket = "receipts";

    private readonly IApplicationDbContext _db;
    private readonly ISupabaseStorageClient _storage;

    public GetReceiptSignedUrlQueryHandler(IApplicationDbContext db, ISupabaseStorageClient storage)
    {
        _db = db;
        _storage = storage;
    }

    public async ValueTask<ErrorOr<string>> Handle(GetReceiptSignedUrlQuery query, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(po => po.DealId == query.DealId, cancellationToken);
        if (po is null || string.IsNullOrEmpty(po.ReceiptUrl))
            return Error.NotFound(description: "No receipt has been uploaded for this deal.");

        try
        {
            var signedUrl = await _storage.CreateSignedUrlAsync(ReceiptsBucket, po.ReceiptUrl, 3600, cancellationToken);
            return signedUrl;
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Failure(description: ex.Message);
        }
    }
}
