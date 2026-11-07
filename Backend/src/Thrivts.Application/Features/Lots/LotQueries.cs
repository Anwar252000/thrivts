using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Lots;

public sealed record LotSummaryDto(
    Guid Id, string Serial, Guid SupplierId, string SupplierName, int Sacks, DateOnly ArrivalDate, LotStage Stage,
    int ItemCount, int Received, int QtyA, int QtyB, int QtyC, int Rejected,
    DateTimeOffset GatedAt, DateTimeOffset CountDueAt, DateTimeOffset? CountFinalizedAt, DateTimeOffset? GradeDueAt, int GradingDays,
    DateTimeOffset? SupervisorSignedAt, DateTimeOffset? CosignedAt, DateTimeOffset? SentBackAt, string? SentBackReason);

public sealed record LotItemDto(
    Guid Id, Guid? ItemId, string ItemName, int Received, int QtyA, int QtyB, int QtyC, int Rejected,
    RejectionReason? RejectionReason, int ReturnedRejected, int Unaccounted, int SortOrder, int WashRejected);

public sealed record LotDetailDto(
    LotSummaryDto Summary, IReadOnlyList<LotItemDto> Items, DateTimeOffset? GradeDraftSavedAt,
    Guid? SupervisorSignatureId, string? SupervisorSignedBy, Guid? CosignSignatureId, string? CosignedBy, string? SentBackBy);

public enum LotScope
{
    /// <summary>Everything not yet co-signed (the supervisor's working list).</summary>
    Open,

    /// <summary>Signed by the supervisor, waiting for co-sign.</summary>
    AwaitingCosign,

    /// <summary>Being counted or graded and already has items (co-signer's read-only view).</summary>
    InProgress,

    /// <summary>Co-signed into stock, newest first.</summary>
    Finalized,
}

internal static class LotProjections
{
    public static IQueryable<LotSummaryDto> ToSummaries(this IQueryable<Lot> lots) => lots.Select(l => new LotSummaryDto(
        l.Id, l.Serial, l.SupplierId, l.SupplierName, l.Sacks, l.ArrivalDate, l.Stage,
        l.Items.Count, l.Items.Sum(i => i.Received), l.Items.Sum(i => i.QtyA), l.Items.Sum(i => i.QtyB), l.Items.Sum(i => i.QtyC), l.Items.Sum(i => i.Rejected),
        l.GatedAt, l.GatedAt.AddHours(24), l.CountFinalizedAt, null, 1,
        l.SupervisorSignedAt, l.CosignedAt, l.SentBackAt, l.SentBackReason));

    /// <summary>The grading deadline depends on the received total, so it is completed in memory
    /// after the page is loaded (same rule as <see cref="Lot.GradingDays"/>).</summary>
    public static LotSummaryDto WithDeadlines(this LotSummaryDto s)
    {
        var days = Math.Max(1, (int)Math.Ceiling(s.Received / (double)Lot.PiecesPerGradingDay));
        return s with { GradingDays = days, GradeDueAt = s.CountFinalizedAt?.AddDays(days) };
    }
}

public sealed record GetLotsQuery(LotScope Scope, string? Search, string? Cursor, int? Size) : IQuery<CursorPage<LotSummaryDto>>;

public sealed class GetLotsHandler(IAppDbContext db) : IQueryHandler<GetLotsQuery, CursorPage<LotSummaryDto>>
{
    public async ValueTask<CursorPage<LotSummaryDto>> Handle(GetLotsQuery query, CancellationToken ct)
    {
        var size = CursorPage<LotSummaryDto>.NormalizeSize(query.Size);
        var cursor = Cursor.Decode(query.Cursor);
        var lots = db.Lots.AsNoTracking();

        lots = query.Scope switch
        {
            LotScope.Open => lots.Where(l => l.Stage != LotStage.Finalized),
            LotScope.AwaitingCosign => lots.Where(l => l.Stage == LotStage.AwaitingCosign),
            LotScope.InProgress => lots.Where(l => (l.Stage == LotStage.Counting || l.Stage == LotStage.Grading) && l.Items.Any()),
            LotScope.Finalized => lots.Where(l => l.Stage == LotStage.Finalized),
            _ => lots,
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            lots = lots.Where(l => l.Serial.ToLower().Contains(term) || l.SupplierName.ToLower().Contains(term));
        }

        if (cursor is { } c)
            lots = lots.Where(l => l.GatedAt < c.At || (l.GatedAt == c.At && l.Id.CompareTo(c.Id) < 0));

        var page = await lots
            .OrderByDescending(l => l.GatedAt).ThenByDescending(l => l.Id)
            .Take(size)
            .ToSummaries()
            .ToListAsync(ct);

        var items = page.Select(s => s.WithDeadlines()).ToList();
        var next = items.Count == size ? new Cursor(items[^1].GatedAt, items[^1].Id).Encode() : null;
        return new CursorPage<LotSummaryDto>(items, next);
    }
}

public sealed record GetLotQuery(Guid LotId) : IQuery<ErrorOr<LotDetailDto>>;

public sealed class GetLotHandler(IAppDbContext db) : IQueryHandler<GetLotQuery, ErrorOr<LotDetailDto>>
{
    public async ValueTask<ErrorOr<LotDetailDto>> Handle(GetLotQuery query, CancellationToken ct)
    {
        var summary = await db.Lots.AsNoTracking().Where(l => l.Id == query.LotId).ToSummaries().FirstOrDefaultAsync(ct);
        if (summary is null)
            return Error.NotFound(description: "Lot not found.");

        var lot = await db.Lots.AsNoTracking()
            .Where(l => l.Id == query.LotId)
            .Select(l => new { l.GradeDraftSavedAt, l.SupervisorSignatureId, l.SupervisorSignedById, l.CosignSignatureId, l.CosignedById, l.SentBackById })
            .FirstAsync(ct);

        var items = await db.LotItems.AsNoTracking()
            .Where(i => i.LotId == query.LotId)
            .OrderBy(i => i.SortOrder)
            .Select(i => new LotItemDto(i.Id, i.ItemId, i.ItemName, i.Received, i.QtyA, i.QtyB, i.QtyC, i.Rejected,
                i.RejectionReason, i.ReturnedRejected, i.Received - (i.QtyA + i.QtyB + i.QtyC + i.Rejected), i.SortOrder, i.WashRejected))
            .ToListAsync(ct);

        var userIds = new[] { lot.SupervisorSignedById, lot.CosignedById, lot.SentBackById }.Where(id => id.HasValue).Select(id => id!.Value).ToList();
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        string? Name(Guid? id) => id is { } v && names.TryGetValue(v, out var n) ? n : null;

        return new LotDetailDto(summary.WithDeadlines(), items, lot.GradeDraftSavedAt,
            lot.SupervisorSignatureId, Name(lot.SupervisorSignedById), lot.CosignSignatureId, Name(lot.CosignedById), Name(lot.SentBackById));
    }
}
