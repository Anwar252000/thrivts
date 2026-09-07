using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Sellers;

/// <summary>
/// Powers the seller portal's top stat bar — mirrors seller.html's renderStats(), which
/// deliberately reads live from seller_deals_view (the SAME source as My Deals) rather than the
/// sellers.* aggregate columns, since those are never maintained by a trigger. A deal with a
/// per-seller DealAllocation contributes its allocation row; a deal that names the seller directly
/// (SellerId) with no DealAllocation falls back to the deal's own totals — the same COALESCE the
/// live view performs.
/// </summary>
public sealed record GetSellerDashboardQuery : IQuery<ErrorOr<SellerDashboardDto>>;

public sealed record SellerDashboardDto(long TotalPcsFulfilled, int TotalOrdersFulfilled, decimal TotalPaidUsd);

public sealed class GetSellerDashboardQueryHandler : IQueryHandler<GetSellerDashboardQuery, ErrorOr<SellerDashboardDto>>
{
    private static readonly DealStatus[] DeliveredPlus = [DealStatus.Delivered, DealStatus.Settled];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSellerDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<SellerDashboardDto>> Handle(GetSellerDashboardQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var sellerId = _currentUser.UserId.Value;

        var allocations = await _db.DealAllocations.AsNoTracking()
            .Where(da => da.SellerId == sellerId)
            .Select(da => new { da.DealId, da.AllocatedQuantityPcs, da.TotalPayoutUsd, da.PayoutPaidAt })
            .ToListAsync(cancellationToken);
        var allocatedDealIds = allocations.Select(a => a.DealId).ToHashSet();

        var directDeals = await _db.Deals.AsNoTracking()
            .Where(d => d.SellerId == sellerId && !allocatedDealIds.Contains(d.Id))
            .Select(d => new { DealId = d.Id, AllocatedQuantityPcs = d.TotalQuantityPcs, TotalPayoutUsd = d.TotalSellerPayoutUsd, PayoutPaidAt = (DateTimeOffset?)null })
            .ToListAsync(cancellationToken);

        var dealIds = allocations.Select(a => a.DealId).Concat(directDeals.Select(d => d.DealId)).ToArray();
        var statusByDealId = await _db.Deals.AsNoTracking()
            .Where(d => dealIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Status })
            .ToDictionaryAsync(x => x.Id, x => x.Status, cancellationToken);

        long pcs = 0;
        decimal paid = 0;
        var fulfilledDealIds = new HashSet<Guid>();

        foreach (var row in allocations.Select(a => (a.DealId, a.AllocatedQuantityPcs, a.TotalPayoutUsd, a.PayoutPaidAt))
                     .Concat(directDeals.Select(d => (d.DealId, d.AllocatedQuantityPcs, d.TotalPayoutUsd, d.PayoutPaidAt))))
        {
            var status = statusByDealId.GetValueOrDefault(row.DealId);

            if (DeliveredPlus.Contains(status))
            {
                pcs += row.AllocatedQuantityPcs;
                fulfilledDealIds.Add(row.DealId);
            }

            if (row.PayoutPaidAt is not null)
                paid += row.TotalPayoutUsd;
        }

        return new SellerDashboardDto(pcs, fulfilledDealIds.Count, paid);
    }
}
