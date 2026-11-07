using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Inventory;

namespace Thrivts.Application.Features.Inventory;

public sealed record InventoryItemRow(
    Guid StockBalanceId, string ItemName, Grade Grade, int QuantityIn, int DispatchedContainer, int DispatchedSmall,
    int DispatchedSku, int InSacks, int OnHand, int Available, int AtWash, int WashIn, int WashLost);

public sealed record InventoryLotDto(
    Guid LotId, string LotSerial, string SupplierName, DateOnly ArrivalDate,
    int QuantityIn, int DispatchedContainer, int DispatchedSmall, int DispatchedSku, int InSacks, int OnHand, int AtWash, int WashIn, int WashLost,
    IReadOnlyList<InventoryItemRow> Items, IReadOnlyList<string> SackCodes);

/// <summary>Zone 3 inventory, one card per lot. Reads the running counters on stock balances, so it
/// stays fast no matter how much history accumulates; paged by lot.</summary>
public sealed record GetInventoryLotsQuery(string? Search, Guid? SupplierId, bool IncludeCleared, int Page, int PageSize) : IQuery<PagedResult<InventoryLotDto>>;

public sealed class GetInventoryLotsHandler(IAppDbContext db) : IQueryHandler<GetInventoryLotsQuery, PagedResult<InventoryLotDto>>
{
    public async ValueTask<PagedResult<InventoryLotDto>> Handle(GetInventoryLotsQuery query, CancellationToken ct)
    {
        var (page, pageSize) = PagedResult<InventoryLotDto>.Normalize(query.Page, query.PageSize, 100);

        var balances = from b in db.StockBalances.AsNoTracking()
                       join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                       select b;

        if (query.SupplierId is { } supplierId)
            balances = balances.Where(b => b.SupplierId == supplierId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            balances = balances.Where(b => b.LotSerial.ToLower().Contains(term) || b.ItemName.ToLower().Contains(term) || b.SupplierName.ToLower().Contains(term));
        }

        var lots = balances
            .GroupBy(b => new { b.LotId, b.LotSerial, b.SupplierName, b.ArrivalDate })
            .Select(g => new
            {
                g.Key.LotId,
                g.Key.LotSerial,
                g.Key.SupplierName,
                g.Key.ArrivalDate,
                QuantityIn = g.Sum(x => x.QuantityIn),
                DispatchedContainer = g.Sum(x => x.DispatchedContainer),
                DispatchedSmall = g.Sum(x => x.DispatchedSmall),
                DispatchedSku = g.Sum(x => x.DispatchedSku),
                InSacks = g.Sum(x => x.InSacks),
                OnHand = g.Sum(x => x.OnHand),
                AtWash = g.Sum(x => x.AtWash),
                WashIn = g.Sum(x => x.WashIn),
                WashLost = g.Sum(x => x.WashLost),
            });

        if (!query.IncludeCleared)
            lots = lots.Where(l => l.OnHand > 0 || l.AtWash > 0);

        var total = await lots.CountAsync(ct);
        var pageLots = await lots
            .OrderByDescending(l => l.ArrivalDate).ThenByDescending(l => l.LotSerial)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var lotIds = pageLots.Select(l => l.LotId).ToList();
        var rows = await db.StockBalances.AsNoTracking()
            .Where(b => lotIds.Contains(b.LotId))
            .OrderBy(b => b.ItemName).ThenBy(b => b.Grade)
            .Select(b => new { b.LotId, Row = new InventoryItemRow(b.Id, b.ItemName, b.Grade, b.QuantityIn, b.DispatchedContainer, b.DispatchedSmall, b.DispatchedSku, b.InSacks, b.OnHand, b.OnHand - b.InSacks, b.AtWash, b.WashIn, b.WashLost) })
            .ToListAsync(ct);

        var sackCodes = await (
                from c in db.SackComponents.AsNoTracking()
                join s in db.Sacks.AsNoTracking() on c.SackId equals s.Id
                where lotIds.Contains(c.LotId) && s.Status == Domain.Sku.SackStatus.Live && c.Quantity > 0
                select new { c.LotId, s.Code })
            .Distinct()
            .ToListAsync(ct);

        var items = pageLots.Select(l => new InventoryLotDto(
            l.LotId, l.LotSerial, l.SupplierName, l.ArrivalDate, l.QuantityIn, l.DispatchedContainer, l.DispatchedSmall, l.DispatchedSku, l.InSacks, l.OnHand, l.AtWash, l.WashIn, l.WashLost,
            rows.Where(r => r.LotId == l.LotId).Select(r => r.Row).ToList(),
            sackCodes.Where(s => s.LotId == l.LotId).Select(s => s.Code).Order().ToList())).ToList();

        return new PagedResult<InventoryLotDto>(items, total, page, pageSize);
    }
}

public sealed record InventorySummaryDto(int OnHand, int Available, int InSacks, int ActiveLots, int ReceivedInRange, int DispatchedInRange, int ReturnedInRange, int AtWash);

public sealed record GetInventorySummaryQuery(DateOnly? From, DateOnly? To) : IQuery<InventorySummaryDto>;

public sealed class GetInventorySummaryHandler(IAppDbContext db) : IQueryHandler<GetInventorySummaryQuery, InventorySummaryDto>
{
    private static readonly StockMovementType[] OutTypes =
        [StockMovementType.ContainerDispatch, StockMovementType.SmallOrderDispatch, StockMovementType.SkuShipment, StockMovementType.LooseDispatch, StockMovementType.DispatchReversal];

    public async ValueTask<InventorySummaryDto> Handle(GetInventorySummaryQuery query, CancellationToken ct)
    {
        var balances = from b in db.StockBalances.AsNoTracking()
                       join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                       select b;

        var totals = await balances.GroupBy(_ => 1).Select(g => new
        {
            OnHand = g.Sum(b => b.OnHand),
            InSacks = g.Sum(b => b.InSacks),
            AtWash = g.Sum(b => b.AtWash),
        }).FirstOrDefaultAsync(ct);
        var activeLots = await balances.Where(b => b.OnHand > 0 || b.AtWash > 0).Select(b => b.LotId).Distinct().CountAsync(ct);

        var movements = db.StockMovements.AsNoTracking();
        if (query.From is { } from)
            movements = movements.Where(m => m.BusinessDate >= from);
        if (query.To is { } to)
            movements = movements.Where(m => m.BusinessDate <= to);

        var received = await movements.Where(m => m.Type == StockMovementType.CosignIn || m.Type == StockMovementType.Correction).SumAsync(m => (int?)m.Quantity, ct) ?? 0;
        var dispatched = -(await movements.Where(m => OutTypes.Contains(m.Type)).SumAsync(m => (int?)m.Quantity, ct) ?? 0);

        var returns = db.RejectionReturns.AsNoTracking();
        if (query.From is { } rf)
            returns = returns.Where(r => r.BusinessDate >= rf);
        if (query.To is { } rt)
            returns = returns.Where(r => r.BusinessDate <= rt);
        var returned = await returns.SumAsync(r => (int?)r.TotalPieces, ct) ?? 0;

        var onHand = totals?.OnHand ?? 0;
        var inSacks = totals?.InSacks ?? 0;
        return new InventorySummaryDto(onHand, onHand - inSacks, inSacks, activeLots, received, dispatched, returned, totals?.AtWash ?? 0);
    }
}

public sealed record LotStockDto(Guid StockBalanceId, Guid LotId, string LotSerial, string SupplierName, string ItemName, Grade Grade, int Available, int OnHand);

/// <summary>What can still leave from a lot — feeds the "Add from lot" button on dispatch forms.</summary>
public sealed record GetLotStockQuery(string LotSerial) : IQuery<IReadOnlyList<LotStockDto>>;

public sealed class GetLotStockHandler(IAppDbContext db) : IQueryHandler<GetLotStockQuery, IReadOnlyList<LotStockDto>>
{
    public async ValueTask<IReadOnlyList<LotStockDto>> Handle(GetLotStockQuery query, CancellationToken ct)
    {
        var serial = query.LotSerial.Trim().ToUpperInvariant();
        return await (
                from b in db.StockBalances.AsNoTracking()
                join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                where b.LotSerial == serial && b.OnHand - b.InSacks > 0
                orderby b.ItemName, b.Grade
                select new LotStockDto(b.Id, b.LotId, b.LotSerial, b.SupplierName, b.ItemName, b.Grade, b.OnHand - b.InSacks, b.OnHand))
            .ToListAsync(ct);
    }
}

public sealed record InventoryExportRow(string LotSerial, string SupplierName, DateOnly ArrivalDate, string ItemName, Grade Grade, int QuantityIn, int DispatchedContainer, int DispatchedSmall, int DispatchedSku, int InSacks, int OnHand, int AtWash, int WashIn, int WashLost);

/// <summary>Streams every balance row for the CSV export — rows go straight from the database cursor
/// to the response, so exporting lakhs of rows never holds them all in memory.</summary>
public sealed record ExportInventoryQuery : IStreamQuery<InventoryExportRow>;

public sealed class ExportInventoryHandler(IAppDbContext db) : IStreamQueryHandler<ExportInventoryQuery, InventoryExportRow>
{
    public IAsyncEnumerable<InventoryExportRow> Handle(ExportInventoryQuery query, CancellationToken ct) =>
        (from b in db.StockBalances.AsNoTracking()
         join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
         orderby b.ArrivalDate descending, b.LotSerial, b.ItemName, b.Grade
         select new InventoryExportRow(b.LotSerial, b.SupplierName, b.ArrivalDate, b.ItemName, b.Grade, b.QuantityIn, b.DispatchedContainer, b.DispatchedSmall, b.DispatchedSku, b.InSacks, b.OnHand, b.AtWash, b.WashIn, b.WashLost))
        .AsAsyncEnumerable();
}
