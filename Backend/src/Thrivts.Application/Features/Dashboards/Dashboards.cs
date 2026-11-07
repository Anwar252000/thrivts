using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;
using Thrivts.Domain.Sku;

namespace Thrivts.Application.Features.Dashboards;

public sealed record PipelineCounts(int Counting, int Grading, int AwaitingCosign, int Finalized);

public sealed record SupplierStock(string SupplierName, int Pieces);

public sealed record OperationsOverviewDto(int OpenLots, int PiecesInProcessing, int PiecesInHand, int SuppliersWithStock, PipelineCounts Pipeline, IReadOnlyList<SupplierStock> StockBySupplier);

/// <summary>The GM's live operations overview. Unlike the legacy screen, "pieces in hand" subtracts
/// everything that has already left the warehouse.</summary>
public sealed record GetOperationsOverviewQuery : IQuery<OperationsOverviewDto>;

public sealed class GetOperationsOverviewHandler(IAppDbContext db) : IQueryHandler<GetOperationsOverviewQuery, OperationsOverviewDto>
{
    public async ValueTask<OperationsOverviewDto> Handle(GetOperationsOverviewQuery query, CancellationToken ct)
    {
        var stages = await db.Lots.AsNoTracking().GroupBy(l => l.Stage).Select(g => new { Stage = g.Key, Count = g.Count() }).ToListAsync(ct);
        int Count(LotStage stage) => stages.FirstOrDefault(s => s.Stage == stage)?.Count ?? 0;

        var processing = await (from i in db.LotItems.AsNoTracking()
                                join l in db.Lots.AsNoTracking() on i.LotId equals l.Id
                                where l.Stage != LotStage.Finalized
                                select (int?)i.Received).SumAsync(ct) ?? 0;

        var bySupplier = await (from b in db.StockBalances.AsNoTracking()
                                join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                                where b.OnHand > 0
                                group b by b.SupplierName into g
                                select new SupplierStock(g.Key, g.Sum(b => b.OnHand)))
            .OrderByDescending(s => s.Pieces)
            .ToListAsync(ct);

        return new OperationsOverviewDto(
            stages.Where(s => s.Stage != LotStage.Finalized).Sum(s => s.Count),
            processing,
            bySupplier.Sum(s => s.Pieces),
            bySupplier.Count,
            new PipelineCounts(Count(LotStage.Counting), Count(LotStage.Grading), Count(LotStage.AwaitingCosign), Count(LotStage.Finalized)),
            bySupplier);
    }
}

public sealed record RangeKpis(int Received, int Rejected, int Finalized, int Dispatched, int Shipped, int Returned, int SentToWash, int WashedBack, int WashRejected);

public sealed record RightNowKpis(int InCustody, int Available, int LiveSacks, int PiecesInSacks, int AssignedSacks, int WarehouseSacks, int AtWash);

public sealed record FunnelKpis(int Received, int Finalized, int InSacks, int Assigned, int Shipped);

public sealed record InventoryDashboardDto(DateOnly From, DateOnly To, RangeKpis Range, RightNowKpis Now, FunnelKpis Funnel);

/// <summary>The inventory manager's dashboard: what happened in a date range, what is in custody
/// right now, and the all-time funnel from gate to shipment.</summary>
public sealed record GetInventoryDashboardQuery(DateOnly? From, DateOnly? To) : IQuery<InventoryDashboardDto>;

public sealed class GetInventoryDashboardHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetInventoryDashboardQuery, InventoryDashboardDto>
{
    private static readonly TimeSpan Pakistan = TimeSpan.FromHours(5);

    public async ValueTask<InventoryDashboardDto> Handle(GetInventoryDashboardQuery query, CancellationToken ct)
    {
        var to = query.To ?? clock.Today;
        var from = query.From ?? new DateOnly(to.Year, to.Month, 1);
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), Pakistan).ToUniversalTime();
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), Pakistan).ToUniversalTime();

        var lotItems = from i in db.LotItems.AsNoTracking()
                       join l in db.Lots.AsNoTracking() on i.LotId equals l.Id
                       select new { i, l };

        var received = await lotItems.Where(x => x.l.ArrivalDate >= from && x.l.ArrivalDate <= to).SumAsync(x => (int?)(x.i.QtyA + x.i.QtyB + x.i.QtyC), ct) ?? 0;
        var rejected = await lotItems.Where(x => x.l.ArrivalDate >= from && x.l.ArrivalDate <= to).SumAsync(x => (int?)x.i.Rejected, ct) ?? 0;
        var finalized = await lotItems.Where(x => x.l.Stage == LotStage.Finalized && x.l.CosignedAt >= start && x.l.CosignedAt < end).SumAsync(x => (int?)(x.i.QtyA + x.i.QtyC), ct) ?? 0;

        var outTypes = new[] { StockMovementType.ContainerDispatch, StockMovementType.SmallOrderDispatch, StockMovementType.DispatchReversal };
        var dispatched = -(await db.StockMovements.AsNoTracking().Where(m => outTypes.Contains(m.Type) && m.BusinessDate >= from && m.BusinessDate <= to).SumAsync(m => (int?)m.Quantity, ct) ?? 0);
        var shipped = await db.SkuDispatches.AsNoTracking().Where(d => d.OccurredAt >= start && d.OccurredAt < end).SumAsync(d => (int?)d.Quantity, ct) ?? 0;
        var returned = await db.RejectionReturns.AsNoTracking().Where(r => r.BusinessDate >= from && r.BusinessDate <= to).SumAsync(r => (int?)r.TotalPieces, ct) ?? 0;

        var sentToWash = -(await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.WashOut && m.BusinessDate >= from && m.BusinessDate <= to).SumAsync(m => (int?)m.Quantity, ct) ?? 0);
        var washedBack = await db.StockMovements.AsNoTracking()
            .Where(m => (m.Type == StockMovementType.WashReturn || m.Type == StockMovementType.WashRegrade) && m.BusinessDate >= from && m.BusinessDate <= to)
            .SumAsync(m => (int?)m.Quantity, ct) ?? 0;
        var washRejected = await db.WashBatches.AsNoTracking()
            .Where(w => w.ReceivedDate >= from && w.ReceivedDate <= to)
            .SelectMany(w => w.Lines)
            .SumAsync(l => (int?)l.Rejected, ct) ?? 0;

        var custody = await (from b in db.StockBalances.AsNoTracking()
                             join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                             group b by 1 into g
                             select new { OnHand = g.Sum(b => b.OnHand), InSacks = g.Sum(b => b.InSacks), In = g.Sum(b => b.QuantityIn), AtWash = g.Sum(b => b.AtWash) })
            .FirstOrDefaultAsync(ct);

        var sacks = await db.Sacks.AsNoTracking().Where(s => s.Status == SackStatus.Live)
            .GroupBy(s => s.Destination).Select(g => new { g.Key, Count = g.Count(), Pieces = g.Sum(s => s.QtyA + s.QtyC) }).ToListAsync(ct);

        var allReceived = await lotItems.SumAsync(x => (int?)(x.i.QtyA + x.i.QtyB + x.i.QtyC), ct) ?? 0;
        var allShipped = await db.SkuDispatches.AsNoTracking().SumAsync(d => (int?)d.Quantity, ct) ?? 0;
        var assignedPieces = sacks.Where(s => s.Key == SackDestination.Order).Sum(s => s.Pieces);

        return new InventoryDashboardDto(from, to,
            new RangeKpis(received, rejected, finalized, dispatched, shipped, returned, sentToWash, washedBack, washRejected),
            new RightNowKpis((custody?.OnHand ?? 0) + (custody?.AtWash ?? 0), (custody?.OnHand ?? 0) - (custody?.InSacks ?? 0), sacks.Sum(s => s.Count), custody?.InSacks ?? 0,
                sacks.Where(s => s.Key == SackDestination.Order).Sum(s => s.Count), sacks.Where(s => s.Key == SackDestination.Warehouse).Sum(s => s.Count),
                custody?.AtWash ?? 0),
            new FunnelKpis(allReceived, custody?.In ?? 0, custody?.InSacks ?? 0, assignedPieces, allShipped));
    }
}
