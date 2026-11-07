using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Sku;

namespace Thrivts.Application.Features.Sku;

public sealed record PoolLotDto(Guid StockBalanceId, string LotSerial, Grade Grade, int Available);

public sealed record PoolItemDto(string ItemName, string? Prefix, string SkuCategory, int SackSize, int AvailableA, int AvailableC, IReadOnlyList<PoolLotDto> Lots);

public sealed record SkuSummaryDto(int PoolA, int PoolC, int LiveSacks, int PiecesInSacks, int AssignedSacks, int WarehouseSacks, int DispatchedPieces);

internal static class SkuLookups
{
    public static async Task<Dictionary<string, (string Category, int Size)>> CategoriesForAsync(IAppDbContext db, IEnumerable<string> normalizedItems, CancellationToken ct)
    {
        var keys = normalizedItems.Distinct().ToList();
        var itemCategories = await db.Items.AsNoTracking()
            .Where(i => keys.Contains(i.NormalizedName))
            .Select(i => new { i.NormalizedName, i.Name, i.SkuCategory })
            .ToListAsync(ct);
        var sizes = await db.SkuCategories.AsNoTracking().ToDictionaryAsync(c => c.NormalizedName, c => (c.Name, c.SackSize), ct);

        return keys.ToDictionary(k => k, k =>
        {
            var category = itemCategories.FirstOrDefault(i => i.NormalizedName == k)?.SkuCategory ?? SkuCategoryClassifier.Classify(k);
            return sizes.TryGetValue(Domain.Common.Text.Normalize(category), out var s) ? (s.Name, s.SackSize) : (category, SkuCategory.DefaultSackSize);
        });
    }
}

/// <summary>The packing pool: finalized pieces not yet in a sack, per item, with the lots they sit in.</summary>
public sealed record GetPoolQuery(string? Search) : IQuery<IReadOnlyList<PoolItemDto>>;

public sealed class GetPoolHandler(IAppDbContext db) : IQueryHandler<GetPoolQuery, IReadOnlyList<PoolItemDto>>
{
    public async ValueTask<IReadOnlyList<PoolItemDto>> Handle(GetPoolQuery query, CancellationToken ct)
    {
        var rows = from b in db.StockBalances.AsNoTracking()
                   join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                   where b.OnHand - b.InSacks > 0
                   select b;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            rows = rows.Where(b => b.NormalizedItemName.Contains(term) || b.LotSerial.ToLower().Contains(term));
        }

        var balances = await rows
            .OrderBy(b => b.ArrivalDate).ThenBy(b => b.LotSerial)
            .Select(b => new { b.Id, b.ItemName, b.NormalizedItemName, b.LotSerial, b.Grade, Available = b.OnHand - b.InSacks })
            .ToListAsync(ct);

        var keys = balances.Select(b => b.NormalizedItemName).Distinct().ToList();
        var categories = await SkuLookups.CategoriesForAsync(db, keys, ct);
        var prefixes = await db.SkuPrefixes.AsNoTracking().Where(p => keys.Contains(p.NormalizedItemName)).ToDictionaryAsync(p => p.NormalizedItemName, p => p.Prefix, ct);

        return balances
            .GroupBy(b => b.NormalizedItemName)
            .Select(g =>
            {
                var (category, size) = categories[g.Key];
                return new PoolItemDto(
                    g.First().ItemName,
                    prefixes.GetValueOrDefault(g.Key),
                    category,
                    size,
                    g.Where(b => b.Grade == Grade.A).Sum(b => b.Available),
                    g.Where(b => b.Grade == Grade.C).Sum(b => b.Available),
                    g.Select(b => new PoolLotDto(b.Id, b.LotSerial, b.Grade, b.Available)).ToList());
            })
            .OrderByDescending(p => p.AvailableA + p.AvailableC)
            .ToList();
    }
}

public sealed record GetSkuSummaryQuery : IQuery<SkuSummaryDto>;

public sealed class GetSkuSummaryHandler(IAppDbContext db) : IQueryHandler<GetSkuSummaryQuery, SkuSummaryDto>
{
    public async ValueTask<SkuSummaryDto> Handle(GetSkuSummaryQuery query, CancellationToken ct)
    {
        var pool = await (from b in db.StockBalances.AsNoTracking()
                          join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                          group b by b.Grade into g
                          select new { Grade = g.Key, Available = g.Sum(b => b.OnHand - b.InSacks) })
            .ToListAsync(ct);

        var sacks = await db.Sacks.AsNoTracking().Where(s => s.Status == SackStatus.Live)
            .GroupBy(s => s.Destination)
            .Select(g => new { Destination = g.Key, Count = g.Count(), Pieces = g.Sum(s => s.QtyA + s.QtyC) })
            .ToListAsync(ct);

        var dispatched = await db.SkuDispatches.AsNoTracking().SumAsync(d => (int?)d.Quantity, ct) ?? 0;

        return new SkuSummaryDto(
            pool.Where(p => p.Grade == Grade.A).Sum(p => p.Available),
            pool.Where(p => p.Grade == Grade.C).Sum(p => p.Available),
            sacks.Sum(s => s.Count),
            sacks.Sum(s => s.Pieces),
            sacks.Where(s => s.Destination == SackDestination.Order).Sum(s => s.Count),
            sacks.Where(s => s.Destination == SackDestination.Warehouse).Sum(s => s.Count),
            dispatched);
    }
}

public sealed record SackComponentDto(string LotSerial, string ItemName, Grade Grade, int Quantity);

public sealed record SackDto(
    Guid Id, string Code, string ItemName, string SkuCategory, int Size, bool IsCustom, int QtyA, int QtyC, SackStatus Status,
    SackDestination Destination, Guid? ClientOrderId, Guid? OrderLineId, string? OrderName, DateTimeOffset PackedAt,
    bool CriteriaConfirmed, string? CriteriaNote, IReadOnlyList<SackComponentDto> Components)
{
    public int Pieces => QtyA + QtyC;

    public bool IsOpened => Pieces < Size;
}

public enum SackSort
{
    Number,
    OpenedFirst,
}

/// <summary>Live sacks, optionally for one item. Sorted by item then the numeric tail of the code,
/// so CDP2 comes before CDP10 (the legacy numeric sort).</summary>
public sealed record GetSacksQuery(string? ItemName, SackDestination? Destination, SackSort Sort, int Page, int PageSize) : IQuery<PagedResult<SackDto>>;

public sealed class GetSacksHandler(IAppDbContext db) : IQueryHandler<GetSacksQuery, PagedResult<SackDto>>
{
    public async ValueTask<PagedResult<SackDto>> Handle(GetSacksQuery query, CancellationToken ct)
    {
        var (page, pageSize) = PagedResult<SackDto>.Normalize(query.Page, query.PageSize, 500);
        var sacks = db.Sacks.AsNoTracking().Where(s => s.Status == SackStatus.Live);
        if (!string.IsNullOrWhiteSpace(query.ItemName))
        {
            var key = Domain.Common.Text.Normalize(query.ItemName);
            sacks = sacks.Where(s => s.NormalizedItemName == key);
        }

        if (query.Destination is { } destination)
            sacks = sacks.Where(s => s.Destination == destination);

        var total = await sacks.CountAsync(ct);
        var ordered = query.Sort == SackSort.OpenedFirst
            ? sacks.OrderByDescending(s => s.QtyA + s.QtyC < s.Size).ThenBy(s => s.NormalizedItemName).ThenBy(s => s.Code.Length).ThenBy(s => s.Code)
            : sacks.OrderBy(s => s.NormalizedItemName).ThenBy(s => s.Code.Length).ThenBy(s => s.Code);

        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(SackProjection.ToDto).ToListAsync(ct);
        return new PagedResult<SackDto>(items, total, page, pageSize);
    }
}

public sealed record SackItemFacet(string ItemName, int Count);

public sealed record GetSackItemFacetsQuery : IQuery<IReadOnlyList<SackItemFacet>>;

public sealed class GetSackItemFacetsHandler(IAppDbContext db) : IQueryHandler<GetSackItemFacetsQuery, IReadOnlyList<SackItemFacet>>
{
    public async ValueTask<IReadOnlyList<SackItemFacet>> Handle(GetSackItemFacetsQuery query, CancellationToken ct) =>
        await db.Sacks.AsNoTracking().Where(s => s.Status == SackStatus.Live)
            .GroupBy(s => s.ItemName)
            .Select(g => new SackItemFacet(g.Key, g.Count()))
            .OrderBy(f => f.ItemName)
            .ToListAsync(ct);
}

public sealed record GetSackQuery(Guid Id) : IQuery<ErrorOr<SackDto>>;

public sealed class GetSackHandler(IAppDbContext db) : IQueryHandler<GetSackQuery, ErrorOr<SackDto>>
{
    public async ValueTask<ErrorOr<SackDto>> Handle(GetSackQuery query, CancellationToken ct)
    {
        var sack = await db.Sacks.AsNoTracking().Where(s => s.Id == query.Id).Select(SackProjection.ToDto).FirstOrDefaultAsync(ct);
        return sack is null ? Error.NotFound(description: "Sack not found.") : sack;
    }
}

public sealed record SackLabelDto(string Code, string ItemName, string SkuCategory, int QtyA, int QtyC, int Pieces, int Size, string Lots, string? Includes, string? Note, string QrPayload);

/// <summary>Everything needed to print sack labels. The QR payload keeps the legacy format so
/// existing scanners and habits keep working: VHW|SKU:code|A:n|C:n|TOTAL:n|LOTS:lot:grade:pcs,...</summary>
public sealed record GetSackLabelsQuery(IReadOnlyList<Guid> SackIds) : IQuery<IReadOnlyList<SackLabelDto>>;

public sealed class GetSackLabelsHandler(IAppDbContext db) : IQueryHandler<GetSackLabelsQuery, IReadOnlyList<SackLabelDto>>
{
    public async ValueTask<IReadOnlyList<SackLabelDto>> Handle(GetSackLabelsQuery query, CancellationToken ct)
    {
        var ids = query.SackIds.Take(500).ToList();
        var sacks = await db.Sacks.AsNoTracking().Where(s => ids.Contains(s.Id)).OrderBy(s => s.Code).Select(SackProjection.ToDto).ToListAsync(ct);

        return sacks.Select(s =>
        {
            var lots = string.Join(",", s.Components.Select(c => $"{c.LotSerial}:{c.Grade}:{c.Quantity}"));
            var includes = s.Components.Where(c => !string.Equals(c.ItemName, s.ItemName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => new { c.ItemName, c.Grade })
                .Select(g => $"{g.Sum(c => c.Quantity)} {g.Key.ItemName} {g.Key.Grade}")
                .ToList();

            return new SackLabelDto(s.Code, s.ItemName, s.SkuCategory, s.QtyA, s.QtyC, s.Pieces, s.Size,
                string.Join(", ", s.Components.GroupBy(c => c.LotSerial).Select(g => g.Key)),
                includes.Count == 0 ? null : string.Join("; ", includes),
                s.CriteriaNote,
                $"VHW|SKU:{s.Code}|A:{s.QtyA}|C:{s.QtyC}|TOTAL:{s.Pieces}|LOTS:{lots}");
        }).ToList();
    }
}

public sealed record SkuDispatchDto(Guid Id, SkuDispatchKind Kind, string ItemName, Grade? Grade, int Quantity, string? Reference, string? OrderName, string? SackCode, DateTimeOffset OccurredAt);

public sealed record GetSkuDispatchesQuery(string? Cursor, int? Size) : IQuery<CursorPage<SkuDispatchDto>>;

public sealed class GetSkuDispatchesHandler(IAppDbContext db) : IQueryHandler<GetSkuDispatchesQuery, CursorPage<SkuDispatchDto>>
{
    public async ValueTask<CursorPage<SkuDispatchDto>> Handle(GetSkuDispatchesQuery query, CancellationToken ct)
    {
        var size = CursorPage<SkuDispatchDto>.NormalizeSize(query.Size);
        var dispatches = db.SkuDispatches.AsNoTracking();
        if (Cursor.Decode(query.Cursor) is { } c)
            dispatches = dispatches.Where(d => d.OccurredAt < c.At || (d.OccurredAt == c.At && d.Id.CompareTo(c.Id) < 0));

        var page = await dispatches.OrderByDescending(d => d.OccurredAt).ThenByDescending(d => d.Id).Take(size)
            .Select(d => new SkuDispatchDto(d.Id, d.Kind, d.ItemName, d.Grade, d.Quantity, d.Reference, d.OrderName, d.SackCode, d.OccurredAt))
            .ToListAsync(ct);
        var next = page.Count == size ? new Cursor(page[^1].OccurredAt, page[^1].Id).Encode() : null;
        return new CursorPage<SkuDispatchDto>(page, next);
    }
}

internal static class SackProjection
{
    public static readonly System.Linq.Expressions.Expression<Func<Sack, SackDto>> ToDto = s => new SackDto(
        s.Id, s.Code, s.ItemName, s.SkuCategory, s.Size, s.IsCustom, s.QtyA, s.QtyC, s.Status, s.Destination,
        s.ClientOrderId, s.OrderLineId, s.OrderName, s.PackedAt, s.CriteriaConfirmed, s.CriteriaNote,
        s.Components.Where(c => c.Quantity > 0).Select(c => new SackComponentDto(c.LotSerial, c.ItemName, c.Grade, c.Quantity)).ToList());
}
