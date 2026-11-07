using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Inventory;

/// <summary>Rejected = found at grading plus ruined at the washer (WashRejected of them).</summary>
public sealed record PendingRejectionLine(Guid LotItemId, string LotSerial, string ItemName, int Rejected, int Returned, int Pending, RejectionReason? Reason, int WashRejected);

public sealed record PendingRejectionGroup(Guid SupplierId, string SupplierName, int TotalPending, IReadOnlyList<PendingRejectionLine> Lines);

/// <summary>Rejected pieces still waiting to go back, grouped by supplier — finalized lots only.</summary>
public sealed record GetPendingRejectionsQuery : IQuery<IReadOnlyList<PendingRejectionGroup>>;

public sealed class GetPendingRejectionsHandler(IAppDbContext db) : IQueryHandler<GetPendingRejectionsQuery, IReadOnlyList<PendingRejectionGroup>>
{
    public async ValueTask<IReadOnlyList<PendingRejectionGroup>> Handle(GetPendingRejectionsQuery query, CancellationToken ct)
    {
        var rows = await (
                from i in db.LotItems.AsNoTracking()
                join l in db.Lots.AsNoTracking() on i.LotId equals l.Id
                where l.Stage == LotStage.Finalized && i.Rejected + i.WashRejected > i.ReturnedRejected
                orderby l.SupplierName, l.ArrivalDate, i.SortOrder
                select new { l.SupplierId, l.SupplierName, Line = new PendingRejectionLine(i.Id, l.Serial, i.ItemName, i.Rejected + i.WashRejected, i.ReturnedRejected,
                    i.Rejected + i.WashRejected - i.ReturnedRejected, i.RejectionReason, i.WashRejected) })
            .ToListAsync(ct);

        return rows.GroupBy(r => new { r.SupplierId, r.SupplierName })
            .Select(g => new PendingRejectionGroup(g.Key.SupplierId, g.Key.SupplierName, g.Sum(r => r.Line.Pending), g.Select(r => r.Line).ToList()))
            .ToList();
    }
}

public sealed record ReturnLineInput(Guid LotItemId, int Quantity);

public sealed record RejectionReturnDto(Guid Id, string SlipNumber, DateOnly Date, string SupplierName, int Sacks, ReturnReason Reason, string? Notes, int TotalPieces, DateTimeOffset CreatedAt,
    IReadOnlyList<RejectionReturnLineDto> Lines);

public sealed record RejectionReturnLineDto(string LotSerial, string ItemName, int Quantity);

public sealed record CreateRejectionReturnCommand(string SlipNumber, DateOnly? Date, int Sacks, ReturnReason Reason, string? Notes, IReadOnlyList<ReturnLineInput> Lines)
    : ICommand<ErrorOr<Guid>>;

public sealed class CreateRejectionReturnValidator : AbstractValidator<CreateRejectionReturnCommand>
{
    public CreateRejectionReturnValidator()
    {
        RuleFor(x => x.SlipNumber).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Sacks).InclusiveBetween(0, 10_000);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= 500);
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.LotItemId).NotEmpty();
            l.RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
        });
    }
}

/// <summary>Records rejected pieces going back to one supplier against their slip. Updates each lot
/// item's returned count (never net stock — rejected pieces were never in it).</summary>
public sealed class CreateRejectionReturnHandler(IAppDbContext db, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<CreateRejectionReturnCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(CreateRejectionReturnCommand command, CancellationToken ct)
    {
        var lineIds = command.Lines.Select(l => l.LotItemId).Distinct().ToList();
        var items = await (
                from i in db.LotItems
                join l in db.Lots on i.LotId equals l.Id
                where lineIds.Contains(i.Id) && l.Stage == LotStage.Finalized
                select new { Item = i, l.Serial, l.SupplierId, l.SupplierName })
            .ToListAsync(ct);

        if (items.Count != lineIds.Count)
            return Error.NotFound(description: "Some lines are no longer available to return — reload and try again.");
        if (items.Select(i => i.SupplierId).Distinct().Count() > 1)
            return Error.Validation(description: "A return slip covers one supplier — select lines from a single supplier.");

        var supplier = items[0];
        var ret = new RejectionReturn(command.SlipNumber, command.Date ?? clock.Today, supplier.SupplierId, supplier.SupplierName, command.Sacks, command.Reason, command.Notes?.Trim());
        foreach (var line in command.Lines.GroupBy(l => l.LotItemId).Select(g => new { g.Key, Quantity = g.Sum(x => x.Quantity) }))
        {
            var source = items.First(i => i.Item.Id == line.Key);
            source.Item.RecordReturn(line.Quantity);
            ret.AddLine(source.Item.Id, source.Serial, source.Item.ItemName, line.Quantity);
        }

        db.RejectionReturns.Add(ret);
        activity.Record(ActivityArea.Inventory, "returned", $"Slip {ret.SlipNumber}: {ret.TotalPieces:N0} rejected pcs returned to {ret.SupplierName}", ret.Id);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        return ret.Id;
    }
}

public sealed record GetRejectionReturnsQuery(string? Cursor, int? Size) : IQuery<CursorPage<RejectionReturnDto>>;

public sealed class GetRejectionReturnsHandler(IAppDbContext db) : IQueryHandler<GetRejectionReturnsQuery, CursorPage<RejectionReturnDto>>
{
    public async ValueTask<CursorPage<RejectionReturnDto>> Handle(GetRejectionReturnsQuery query, CancellationToken ct)
    {
        var size = CursorPage<RejectionReturnDto>.NormalizeSize(query.Size);
        var returns = db.RejectionReturns.AsNoTracking();
        if (Cursor.Decode(query.Cursor) is { } c)
            returns = returns.Where(r => r.CreatedAt < c.At || (r.CreatedAt == c.At && r.Id.CompareTo(c.Id) < 0));

        var page = await returns.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Take(size)
            .Select(r => new RejectionReturnDto(r.Id, r.SlipNumber, r.BusinessDate, r.SupplierName, r.Sacks, r.Reason, r.Notes, r.TotalPieces, r.CreatedAt,
                r.Lines.Select(l => new RejectionReturnLineDto(l.LotSerial, l.ItemName, l.Quantity)).ToList()))
            .ToListAsync(ct);

        var next = page.Count == size ? new Cursor(page[^1].CreatedAt, page[^1].Id).Encode() : null;
        return new CursorPage<RejectionReturnDto>(page, next);
    }
}
