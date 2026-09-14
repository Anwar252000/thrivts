using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.PurchaseOrders;

/// <summary>Reads the purchase_orders row for a deal — reachable by the deal's buyer, its seller,
/// or any admin. Most rows exist because the deals AFTER-INSERT trigger auto-issued them the moment
/// the deal was created as Confirmed (confirmed empirically against the live DB — see
/// PurchaseOrder.cs's own doc comment); this query is purely a read, no PO is ever created here.</summary>
public sealed record GetPurchaseOrderByDealIdQuery(Guid DealId) : IQuery<ErrorOr<PurchaseOrderDto?>>;

public sealed record PurchaseOrderDto(
    Guid Id, Guid DealId, string PoNumber, PoStatus Status, decimal AmountUsd, string Currency, DateTimeOffset DueAt,
    string? PaymentMethod, string? PaymentLinkUrl, DateTimeOffset? LinkRequestedAt, string? ReceiptUrl,
    DateTimeOffset? PaidMarkedAt, DateTimeOffset? VerifiedAt, string? Notes, DateTimeOffset CreatedAt);

public sealed class GetPurchaseOrderByDealIdQueryHandler : IQueryHandler<GetPurchaseOrderByDealIdQuery, ErrorOr<PurchaseOrderDto?>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPurchaseOrderByDealIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PurchaseOrderDto?>> Handle(GetPurchaseOrderByDealIdQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == query.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{query.DealId}' was not found.");

        var isOwner = deal.BuyerId == _currentUser.UserId || deal.SellerId == _currentUser.UserId;
        if (!isOwner && _currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "You can only view purchase orders on your own deals.");

        var po = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(po => po.DealId == query.DealId, cancellationToken);
        if (po is null)
            return (PurchaseOrderDto?)null;

        return new PurchaseOrderDto(
            po.Id, po.DealId, po.PoNumber, po.Status, po.AmountUsd, po.Currency, po.DueAt,
            po.PaymentMethod, po.PaymentLinkUrl, po.LinkRequestedAt, po.ReceiptUrl,
            po.PaidMarkedAt, po.VerifiedAt, po.Notes, po.CreatedAt);
    }
}
