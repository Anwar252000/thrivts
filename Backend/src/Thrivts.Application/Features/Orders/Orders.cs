using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Orders;

namespace Thrivts.Application.Features.Orders;

public sealed record OrderLineDto(Guid Id, string ItemName, string? Grade, string? Category, int Required, int Ready, int Remaining, OrderLineStatus Status, string? Note, long Seq);

/// <summary>What staff see. No order value, no client login code — those are owner-only.</summary>
public sealed record StaffOrderDto(
    Guid Id, string Name, string? ClientName, ClientOrderStatus Status, DateOnly? Deadline, bool IsCompleted,
    string? DestinationCountry, DateOnly? OrderReceivedDate, DateOnly? DispatchDate, int? Boxes, decimal? WeightKg,
    int Required, int Ready, IReadOnlyList<OrderLineDto> Lines);

public sealed record GetStaffOrdersQuery(bool IncludeCompleted, string? Search) : IQuery<IReadOnlyList<StaffOrderDto>>;

public sealed class GetStaffOrdersHandler(IAppDbContext db) : IQueryHandler<GetStaffOrdersQuery, IReadOnlyList<StaffOrderDto>>
{
    public async ValueTask<IReadOnlyList<StaffOrderDto>> Handle(GetStaffOrdersQuery query, CancellationToken ct)
    {
        var orders = db.ClientOrders.AsNoTracking();
        if (!query.IncludeCompleted)
            orders = orders.Where(o => !o.IsCompleted);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            orders = orders.Where(o => o.NormalizedName.Contains(term) || (o.ClientName != null && o.ClientName.ToLower().Contains(term))
                || o.Lines.Any(l => l.NormalizedItemName.Contains(term)));
        }

        return await orders
            .OrderBy(o => o.Deadline == null).ThenBy(o => o.Deadline).ThenBy(o => o.Name)
            .Take(500)
            .Select(o => new StaffOrderDto(
                o.Id, o.Name, o.ClientName, o.Status, o.Deadline, o.IsCompleted, o.DestinationCountry, o.OrderReceivedDate, o.DispatchDate, o.Boxes, o.WeightKg,
                o.Lines.Sum(l => l.Required), o.Lines.Sum(l => l.Ready < l.Required ? l.Ready : l.Required),
                o.Lines.OrderBy(l => l.Seq).Select(l => new OrderLineDto(l.Id, l.ItemName, l.Grade, l.Category, l.Required, l.Ready, l.Required - l.Ready, l.Status, l.Note, l.Seq)).ToList()))
            .ToListAsync(ct);
    }
}

// ---------- Order editing (GM for staff; owner shares these) ----------

public sealed record CreateOrderCommand(string Name, string? ClientName, DateOnly? Deadline) : ICommand<ErrorOr<Guid>>;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ClientName).MaximumLength(80);
    }
}

public sealed class CreateOrderHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<CreateOrderCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        if (await db.ClientOrders.AnyAsync(o => o.NormalizedName == key, ct))
            return Error.Conflict(description: $"An order called \"{command.Name.Trim()}\" already exists.");

        var order = new ClientOrder(command.Name, command.ClientName?.Trim());
        order.UpdateDetails(command.ClientName?.Trim(), command.Deadline, null, null, null, null, null);
        db.ClientOrders.Add(order);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return order.Id;
    }
}

public sealed record UpdateOrderCommand(
    Guid Id, string Name, string? ClientName, ClientOrderStatus Status, DateOnly? Deadline, string? DestinationCountry,
    DateOnly? OrderReceivedDate, DateOnly? DispatchDate, int? Boxes, decimal? WeightKg) : ICommand<ErrorOr<Success>>;

public sealed class UpdateOrderValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ClientName).MaximumLength(80);
        RuleFor(x => x.DestinationCountry).MaximumLength(60);
        RuleFor(x => x.Boxes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateOrderHandler(IAppDbContext db, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<UpdateOrderCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(UpdateOrderCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.FirstOrDefaultAsync(o => o.Id == command.Id, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        var key = Text.Normalize(command.Name);
        if (key != order.NormalizedName && await db.ClientOrders.AnyAsync(o => o.NormalizedName == key && o.Id != order.Id, ct))
            return Error.Conflict(description: "Another order already has that name.");

        if (order.Status != command.Status)
            activity.Record(ActivityArea.Orders, "status", $"{order.Name} status → {command.Status}", order.Id);

        order.Rename(command.Name);
        order.SetStatus(command.Status);
        order.UpdateDetails(command.ClientName?.Trim(), command.Deadline, command.DestinationCountry?.Trim(), command.OrderReceivedDate, command.DispatchDate, command.Boxes, command.WeightKg);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record DeleteOrderCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteOrderHandler(IAppDbContext db, ICurrentUser user, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<DeleteOrderCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteOrderCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.FirstOrDefaultAsync(o => o.Id == command.Id, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");
        if (await db.Sacks.AnyAsync(s => s.ClientOrderId == order.Id && s.Status == Domain.Sku.SackStatus.Live, ct))
            return Error.Conflict(description: "Sacks are still assigned to this order — unassign them first.");

        order.MarkDeleted(clock.UtcNow, user.UserId);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record SetOrderCompletedCommand(Guid Id, bool Completed) : ICommand<ErrorOr<Success>>;

public sealed class SetOrderCompletedHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<SetOrderCompletedCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetOrderCompletedCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.FirstOrDefaultAsync(o => o.Id == command.Id, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        order.SetCompleted(command.Completed);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record MarkOrderReadyCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class MarkOrderReadyHandler(IAppDbContext db, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<MarkOrderReadyCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(MarkOrderReadyCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.Id, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        order.MarkAllReady();
        activity.Record(ActivityArea.Orders, "all-ready", $"{order.Name}: every line marked ready", order.Id);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

// ---------- Lines ----------

public sealed record SaveOrderLineCommand(Guid OrderId, Guid? LineId, string ItemName, string? Grade, string? Category, int Required, int Ready, OrderLineStatus Status, string? Note)
    : ICommand<ErrorOr<Guid>>;

public sealed class SaveOrderLineValidator : AbstractValidator<SaveOrderLineCommand>
{
    public SaveOrderLineValidator()
    {
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Grade).MaximumLength(20);
        RuleFor(x => x.Category).MaximumLength(60);
        RuleFor(x => x.Required).InclusiveBetween(0, 10_000_000);
        RuleFor(x => x.Ready).InclusiveBetween(0, 10_000_000);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class SaveOrderLineHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<SaveOrderLineCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(SaveOrderLineCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        OrderLine line;
        if (command.LineId is { } lineId)
        {
            line = order.Line(lineId);
            var key = Text.Normalize(command.ItemName);
            if (key != line.NormalizedItemName && order.Lines.Any(l => l.Id != lineId && l.NormalizedItemName == key))
                return Error.Conflict(description: "That item is already on this order.");

            line.SetItem(command.ItemName);
            line.SetQuantities(command.Required, command.Ready);
            line.Update(command.Grade?.Trim(), command.Category?.Trim(), command.Status, command.Note?.Trim());
        }
        else
        {
            line = order.UpsertLine(command.ItemName, command.Grade?.Trim(), command.Category?.Trim(), command.Required, command.Ready, command.Status, command.Note?.Trim());
        }

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return line.Id;
    }
}

public sealed record RemoveOrderLineCommand(Guid OrderId, Guid LineId) : ICommand<ErrorOr<Success>>;

public sealed class RemoveOrderLineHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<RemoveOrderLineCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(RemoveOrderLineCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");
        if (await db.Sacks.AnyAsync(s => s.OrderLineId == command.LineId && s.Status == Domain.Sku.SackStatus.Live, ct))
            return Error.Conflict(description: "Sacks are assigned to this line — unassign them first.");

        order.RemoveLine(command.LineId);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

// ---------- Import (rows parsed from Excel in the browser) ----------

public sealed record ImportLine(string ItemName, string? Grade, int Required, int Ready, string? Status, string? Note);

public sealed record ImportOrder(string Name, string? ClientName, DateOnly? Deadline, IReadOnlyList<ImportLine> Lines);

public sealed record ImportResult(int OrdersCreated, int OrdersUpdated, int LinesUpserted);

/// <summary>Each sheet is an order (sheet name = order and client). Lines merge into existing ones by
/// item — re-importing the same file updates instead of duplicating, and never deletes lines that
/// staff added by hand.</summary>
public sealed record ImportOrdersCommand(IReadOnlyList<ImportOrder> Orders) : ICommand<ImportResult>;

public sealed class ImportOrdersValidator : AbstractValidator<ImportOrdersCommand>
{
    public ImportOrdersValidator()
    {
        RuleFor(x => x.Orders).NotEmpty().Must(o => o.Count <= 100).WithMessage("Import at most 100 sheets at a time.");
        RuleForEach(x => x.Orders).ChildRules(o =>
        {
            o.RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
            o.RuleFor(x => x.Lines).Must(l => l.Count <= ClientOrder.MaxLines);
            o.RuleForEach(x => x.Lines).ChildRules(l =>
            {
                l.RuleFor(x => x.ItemName).NotEmpty().MaximumLength(120);
                l.RuleFor(x => x.Required).InclusiveBetween(0, 10_000_000);
                l.RuleFor(x => x.Ready).InclusiveBetween(0, 10_000_000);
                l.RuleFor(x => x.Note).MaximumLength(500);
            });
        });
    }
}

public sealed class ImportOrdersHandler(IAppDbContext db, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<ImportOrdersCommand, ImportResult>
{
    private static readonly HashSet<string> SectionHeadings = new(StringComparer.OrdinalIgnoreCase)
    {
        "BOTTOMS", "TOPS", "HEADWEAR", "OUTERWEAR", "KNITWEAR", "FOOTWEAR", "ACCESSORIES", "DENIM", "PRODUCT", "ITEMS", "TOTAL", "GRAND TOTAL",
    };

    public async ValueTask<ImportResult> Handle(ImportOrdersCommand command, CancellationToken ct)
    {
        var created = 0;
        var updated = 0;
        var lines = 0;

        await db.InTransactionAsync(async token =>
        {
            foreach (var sheet in command.Orders)
            {
                var key = Text.Normalize(sheet.Name);
                var order = await db.ClientOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.NormalizedName == key, token);
                if (order is null)
                {
                    order = new ClientOrder(sheet.Name, sheet.ClientName ?? sheet.Name);
                    db.ClientOrders.Add(order);
                    created++;
                }
                else
                {
                    updated++;
                }

                if (sheet.Deadline is not null)
                    order.UpdateDetails(order.ClientName, sheet.Deadline, order.DestinationCountry, order.OrderReceivedDate, order.DispatchDate, order.Boxes, order.WeightKg);

                foreach (var line in sheet.Lines)
                {
                    var name = line.ItemName.Trim();
                    if (SectionHeadings.Contains(name) || name.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var status = Enum.TryParse<OrderLineStatus>(line.Status?.Replace(" ", ""), ignoreCase: true, out var s) ? s : (OrderLineStatus?)null;
                    var note = line.Note?.Split('\n')[0].Trim();
                    order.UpsertLine(name, line.Grade?.Trim(), null, line.Required, line.Ready, status, note is { Length: > 60 } ? note[..60] : note);
                    lines++;
                }
            }

            activity.Record(ActivityArea.Orders, "import", $"Imported {command.Orders.Count} order sheet(s): {created} new, {updated} updated, {lines} lines");
            await db.SaveChangesAsync(token);
            return true;
        }, ct);

        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return new ImportResult(created, updated, lines);
    }
}
