using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Sellers;

/// <summary>
/// Replaces the seller_deals_view read that powers My Deals — mirrors renderSellerDealRow()'s
/// fields. Like GetSellerDashboardQuery, a deal with a per-seller DealAllocation contributes that
/// allocation's numbers; a deal that names the seller directly with no DealAllocation row falls
/// back to the deal's own totals (the live view's COALESCE(da.*, d.*)).
/// </summary>
public sealed record GetSellerDealsQuery : IQuery<ErrorOr<List<SellerDealDto>>>;

public sealed record SellerDealDto(
    Guid DealId, string DealNumber, DealStatus Status, int AllocatedQuantityPcs, decimal PricePerPcUsd,
    decimal TotalPayoutUsd, decimal PlatformFeePerPcUsd, DateTimeOffset? PayoutPaidAt,
    DateTimeOffset? ConfirmedAt, DateTimeOffset? PaidAt, DateTimeOffset? InFulfillmentAt, DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt, DateTimeOffset? SettledAt, DateTimeOffset? CancelledAt,
    string? TrackingNumber, string? TrackingUrl, string? Courier,
    string ItemName, GradeType Grade, string DestinationCountry, string? ShippingMode, DateTimeOffset CreatedAt);

public sealed class GetSellerDealsQueryHandler : IQueryHandler<GetSellerDealsQuery, ErrorOr<List<SellerDealDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSellerDealsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<SellerDealDto>>> Handle(GetSellerDealsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var sellerId = _currentUser.UserId.Value;

        var allocations = await _db.DealAllocations.AsNoTracking()
            .Where(da => da.SellerId == sellerId)
            .ToListAsync(cancellationToken);
        var allocByDealId = allocations.ToDictionary(a => a.DealId);
        var allocatedDealIds = allocByDealId.Keys.ToHashSet();

        var deals = await (
            from d in _db.Deals.AsNoTracking()
            join r in _db.Requirements.AsNoTracking() on d.RequirementId equals r.Id
            where allocatedDealIds.Contains(d.Id) || d.SellerId == sellerId
            orderby d.CreatedAt descending
            select new { Deal = d, Requirement = r })
            .ToListAsync(cancellationToken);

        var result = deals.Select(x =>
        {
            var d = x.Deal;
            var alloc = allocByDealId.GetValueOrDefault(d.Id);

            var qty = alloc?.AllocatedQuantityPcs ?? d.TotalQuantityPcs;
            var pricePerPc = alloc?.PricePerPcUsd ?? d.AvgSellerPricePerPcUsd;
            var totalPayout = alloc?.TotalPayoutUsd ?? d.TotalSellerPayoutUsd;
            var feePerPc = alloc?.FeePerPcAppliedUsd ?? 0.70m;

            return new SellerDealDto(
                d.Id, d.DealNumber, d.Status, qty, pricePerPc, totalPayout, feePerPc, alloc?.PayoutPaidAt,
                d.ConfirmedAt, d.PaidAt, d.InFulfillmentAt, d.DispatchedAt, d.DeliveredAt, d.SettledAt, d.CancelledAt,
                d.TrackingNumber, d.TrackingUrl, d.Courier,
                x.Requirement.ItemName, x.Requirement.Grade, x.Requirement.DestinationCountry, x.Requirement.ShippingMode, d.CreatedAt);
        }).ToList();

        return result;
    }
}
