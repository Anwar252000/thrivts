using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;
using Thrivts.Application.Features.Inventory;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Orders;
using Thrivts.Domain.Sku;

namespace Thrivts.Application.Features.Sku;

internal static class DrawQueue
{
    /// <summary>Hands out a FIFO plan piece by piece: sack 1 takes from the oldest lot first, and
    /// the next sack continues where it stopped.</summary>
    public static IEnumerable<(StockBalance Balance, int Quantity)> Take(Queue<(StockBalance Balance, int Remaining)> queue, int quantity)
    {
        var need = quantity;
        while (need > 0)
        {
            var (balance, remaining) = queue.Dequeue();
            var take = Math.Min(remaining, need);
            yield return (balance, take);
            need -= take;
            if (remaining > take)
            {
                var rest = queue.ToList();
                queue.Clear();
                queue.Enqueue((balance, remaining - take));
                foreach (var r in rest)
                    queue.Enqueue(r);
            }
        }
    }

    public static Queue<(StockBalance, int)> From(IReadOnlyList<StockDraw> draws) => new(draws.Select(d => (d.Balance, d.Quantity)));
}

// ---------- Pack ----------

public sealed record PackComponentInput(string ItemName, Grade Grade, int PerSack);

public sealed record PackSacksCommand(
    string ItemName, string? Prefix, int PerSackA, int PerSackC, int SackCount, bool IsCustom,
    IReadOnlyList<PackComponentInput>? Components, bool CriteriaConfirmed, string? CriteriaNote) : ICommand<ErrorOr<IReadOnlyList<string>>>;

public sealed class PackSacksValidator : AbstractValidator<PackSacksCommand>
{
    public PackSacksValidator()
    {
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(Domain.Catalog.Item.MaxNameLength);
        RuleFor(x => x.Prefix).MaximumLength(SkuPrefix.MaxLength);
        RuleFor(x => x.PerSackA).InclusiveBetween(0, 10_000);
        RuleFor(x => x.PerSackC).InclusiveBetween(0, 10_000);
        RuleFor(x => x.SackCount).InclusiveBetween(1, SkuRules.MaxSacksPerBatch).WithMessage($"Pack between 1 and {SkuRules.MaxSacksPerBatch} sacks at a time.");
        RuleFor(x => x.CriteriaConfirmed).Equal(true).WithMessage("Confirm the sack meets the item criteria.");
        RuleFor(x => x.CriteriaNote).MaximumLength(300);
        RuleFor(x => x.Components).Must(c => c is null || c.Count <= 10);
        RuleForEach(x => x.Components).ChildRules(c =>
        {
            c.RuleFor(x => x.ItemName).NotEmpty();
            c.RuleFor(x => x.PerSack).InclusiveBetween(1, 10_000);
        });
        RuleFor(x => x).Must(x => x.PerSackA + x.PerSackC + (x.Components?.Sum(c => c.PerSack) ?? 0) > 0)
            .WithMessage("Each sack needs at least one piece.");
    }
}

/// <summary>
/// Packs N identical sacks of one item (an A/C blend, optionally with components from other items,
/// e.g. "15–20% Denali"). Pieces are drawn oldest-lot-first and committed into the sacks atomically;
/// the per-sack total may not exceed the category's sack size unless it is a custom sack.
/// </summary>
public sealed class PackSacksHandler(IAppDbContext db, ISerialGenerator serials, StockLedger ledger, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<PackSacksCommand, ErrorOr<IReadOnlyList<string>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<string>>> Handle(PackSacksCommand command, CancellationToken ct)
    {
        var itemKey = Text.Normalize(command.ItemName);
        var components = command.Components ?? [];
        if (components.Any(c => Text.Normalize(c.ItemName) == itemKey))
            return Error.Validation(description: "Components must be other items — use the A/C fields for the item itself.");

        var result = await db.InTransactionAsync<ErrorOr<IReadOnlyList<string>>>(async token =>
        {
            var (category, size) = (await SkuLookups.CategoriesForAsync(db, [itemKey], token))[itemKey];
            var perSack = command.PerSackA + command.PerSackC + components.Sum(c => c.PerSack);
            if (!command.IsCustom && perSack > size)
                return Error.Validation(description: $"Per-sack total ({perSack}) exceeds the {category} sack size ({size}) — tick \"Custom sack\" to bag leftovers.");

            var itemName = (await db.StockBalances.AsNoTracking().Where(b => b.NormalizedItemName == itemKey).Select(b => b.ItemName).FirstOrDefaultAsync(token)) ?? command.ItemName;
            var prefix = await ResolvePrefixAsync(itemName, category, command.Prefix, token);
            if (prefix.IsError)
                return prefix.Errors;

            var n = command.SackCount;
            var parts = new List<(int PerSack, Queue<(StockBalance, int)> Queue)>();
            async Task AddPart(string name, Grade grade, int each)
            {
                if (each > 0)
                    parts.Add((each, DrawQueue.From(await ledger.PlanFifoAsync(name, grade, each * n, token))));
            }

            await AddPart(itemName, Grade.A, command.PerSackA);
            await AddPart(itemName, Grade.C, command.PerSackC);
            foreach (var c in components)
                await AddPart(c.ItemName, c.Grade, c.PerSack);

            var now = clock.UtcNow;
            var reserved = new Dictionary<Guid, (StockBalance Balance, int Quantity)>();
            var codes = new List<string>(n);
            for (var i = 0; i < n; i++)
            {
                var code = SkuRules.FormatCode(prefix.Value, await serials.NextAsync(SerialCounters.Sku(prefix.Value), token));
                var sack = new Sack(code, prefix.Value, itemName, category, size, command.IsCustom, command.CriteriaConfirmed, command.CriteriaNote, now);

                foreach (var (each, queue) in parts)
                {
                    foreach (var (balance, quantity) in DrawQueue.Take(queue, each))
                    {
                        sack.AddComponent(balance, quantity);
                        reserved[balance.Id] = reserved.TryGetValue(balance.Id, out var r) ? (balance, r.Quantity + quantity) : (balance, quantity);
                    }
                }

                db.Sacks.Add(sack);
                codes.Add(code);
            }

            foreach (var (balance, quantity) in reserved.Values)
                await ledger.ReserveForSackAsync(balance, quantity, token);

            var lots = string.Join(", ", reserved.Values.Select(r => r.Balance.LotSerial).Distinct());
            activity.Record(ActivityArea.Sku, "pack",
                $"Packed {codes[0]}{(n > 1 ? $"–{codes[^1]}" : "")} ({n} sack{(n > 1 ? "s" : "")}) · {command.PerSackA}A/{command.PerSackC}C each · lots {lots} · " +
                (string.IsNullOrWhiteSpace(command.CriteriaNote) ? "✓ criteria met" : $"⚠ note: {command.CriteriaNote}"));
            await db.SaveChangesAsync(token);
            return codes;
        }, ct);

        if (!result.IsError)
            await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);

        return result;
    }

    /// <summary>An item keeps its prefix forever once assigned. A manual prefix overrides the
    /// suggestion but must not belong to another item.</summary>
    private async Task<ErrorOr<string>> ResolvePrefixAsync(string itemName, string category, string? requested, CancellationToken ct)
    {
        var key = Text.Normalize(itemName);
        var existing = await db.SkuPrefixes.FirstOrDefaultAsync(p => p.NormalizedItemName == key, ct);
        var manual = string.IsNullOrWhiteSpace(requested) ? null : SkuRules.SanitizePrefix(requested);

        if (manual is { Length: > 0 } && manual != existing?.Prefix)
        {
            if (await db.SkuPrefixes.AnyAsync(p => p.Prefix == manual && p.NormalizedItemName != key, ct))
                return Error.Conflict(description: $"Prefix {manual} already belongs to another item.");

            if (existing is null)
                db.SkuPrefixes.Add(new SkuPrefix(itemName, manual));
            else
                existing.Change(manual);
            return manual;
        }

        if (existing is not null)
            return existing.Prefix;

        var taken = (await db.SkuPrefixes.AsNoTracking().Select(p => p.Prefix).ToListAsync(ct)).ToHashSet();
        var suggested = SkuRules.SuggestPrefix(itemName, category, taken);
        db.SkuPrefixes.Add(new SkuPrefix(itemName, suggested));
        return suggested;
    }
}

// ---------- Top up / take out ----------

public sealed record TopUpSackCommand(Guid SackId, int AddA, int AddC) : ICommand<ErrorOr<Success>>;

public sealed class TopUpSackValidator : AbstractValidator<TopUpSackCommand>
{
    public TopUpSackValidator()
    {
        RuleFor(x => x.AddA).InclusiveBetween(0, 10_000);
        RuleFor(x => x.AddC).InclusiveBetween(0, 10_000);
        RuleFor(x => x).Must(x => x.AddA + x.AddC > 0).WithMessage("Add at least one piece.");
    }
}

public sealed class TopUpSackHandler(IAppDbContext db, StockLedger ledger, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<TopUpSackCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(TopUpSackCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var sack = await db.Sacks.Include(s => s.Components).FirstOrDefaultAsync(s => s.Id == command.SackId, token);
            if (sack is null)
                return Error.NotFound(description: "Sack not found.");

            sack.EnsureCanTopUp(command.AddA + command.AddC);
            foreach (var (grade, quantity) in new[] { (Grade.A, command.AddA), (Grade.C, command.AddC) })
            {
                foreach (var draw in await ledger.PlanFifoAsync(sack.ItemName, grade, quantity, token))
                {
                    await ledger.ReserveForSackAsync(draw.Balance, draw.Quantity, token);
                    sack.AddComponent(draw.Balance, draw.Quantity);
                }
            }

            activity.Record(ActivityArea.Sku, "topup", $"Topped up {sack.Code} +{command.AddA}A/{command.AddC}C → {(sack.IsOpened ? $"still {sack.Pieces}/{sack.Size}" : "re-sealed (full)")}", sack.Id);
            await db.SaveChangesAsync(token);
            return Result.Success;
        }, ct);

        if (!result.IsError)
            await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);
        return result;
    }
}

/// <summary>Takes pieces out of a sack back into the pool (the "✂ Take out" action).</summary>
public sealed record TakeOutOfSackCommand(Guid SackId, int TakeA, int TakeC) : ICommand<ErrorOr<Success>>;

public sealed class TakeOutOfSackValidator : AbstractValidator<TakeOutOfSackCommand>
{
    public TakeOutOfSackValidator()
    {
        RuleFor(x => x.TakeA).InclusiveBetween(0, 10_000);
        RuleFor(x => x.TakeC).InclusiveBetween(0, 10_000);
        RuleFor(x => x).Must(x => x.TakeA + x.TakeC > 0).WithMessage("Take out at least one piece.");
    }
}

public sealed class TakeOutOfSackHandler(IAppDbContext db, StockLedger ledger, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<TakeOutOfSackCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(TakeOutOfSackCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var sack = await db.Sacks.Include(s => s.Components).FirstOrDefaultAsync(s => s.Id == command.SackId, token);
            if (sack is null)
                return Error.NotFound(description: "Sack not found.");

            var released = new List<(Guid, int)>();
            if (command.TakeA > 0)
                released.AddRange(sack.TakeOut(Grade.A, command.TakeA));
            if (command.TakeC > 0)
                released.AddRange(sack.TakeOut(Grade.C, command.TakeC));

            foreach (var (balanceId, quantity) in released)
                await ledger.ReleaseFromSackAsync(balanceId, quantity, token);

            activity.Record(ActivityArea.Sku, "return",
                $"Took {command.TakeA}A/{command.TakeC}C out of {sack.Code} back to the pool{(sack.Status == SackStatus.Emptied ? " — sack emptied" : "")}", sack.Id);
            await db.SaveChangesAsync(token);
            return Result.Success;
        }, ct);

        if (!result.IsError)
            await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);
        return result;
    }
}

// ---------- Assign / unassign ----------

public enum SackAssignment
{
    Warehouse,
    Order,
}

/// <summary>Commits a whole sack to the finished-stock warehouse or to an order line. Assigning to
/// an order counts the sack's pieces towards that line's "ready" straight away (nothing leaves
/// inventory until it ships). With NewLine the sack's item is added to the order as a new line.</summary>
public sealed record AssignSackCommand(Guid SackId, SackAssignment Assignment, Guid? ClientOrderId, Guid? OrderLineId, bool NewLine) : ICommand<ErrorOr<Success>>;

public sealed class AssignSackHandler(IAppDbContext db, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<AssignSackCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(AssignSackCommand command, CancellationToken ct)
    {
        var sack = await db.Sacks.FirstOrDefaultAsync(s => s.Id == command.SackId, ct);
        if (sack is null)
            return Error.NotFound(description: "Sack not found.");

        if (command.Assignment == SackAssignment.Warehouse)
        {
            sack.AssignToWarehouse(clock.UtcNow);
            activity.Record(ActivityArea.Sku, "assign", $"{sack.Code} → 🏭 warehouse (finished stock)", sack.Id);
        }
        else
        {
            if (command.ClientOrderId is not { } orderId)
                return Error.Validation(description: "Choose the order.");

            var order = await db.ClientOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null)
                return Error.NotFound(description: "Order not found.");
            if (order.IsCompleted)
                return Error.Validation(description: "That order is already completed.");

            OrderLine line;
            if (command.NewLine)
                line = order.UpsertLine(sack.ItemName, null, sack.SkuCategory, sack.Pieces, 0, OrderLineStatus.Pending, "added at dispatch");
            else if (command.OrderLineId is { } lineId)
                line = order.Line(lineId);
            else
                return Error.Validation(description: "Choose which order line this sack fulfils.");

            line.Commit(sack.Pieces);
            sack.AssignToOrder(order.Id, line.Id, order.Name, clock.UtcNow);
            activity.Record(ActivityArea.Sku, "assign", $"{sack.Code} ({sack.Pieces} pcs) → order {order.Name} · {line.ItemName} ready {line.Ready}/{line.Required}", sack.Id);
        }

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record UnassignSackCommand(Guid SackId) : ICommand<ErrorOr<Success>>;

public sealed class UnassignSackHandler(IAppDbContext db, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<UnassignSackCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(UnassignSackCommand command, CancellationToken ct)
    {
        var sack = await db.Sacks.FirstOrDefaultAsync(s => s.Id == command.SackId, ct);
        if (sack is null)
            return Error.NotFound(description: "Sack not found.");

        if (sack.OrderLineId is { } lineId)
        {
            var line = await db.OrderLines.FirstOrDefaultAsync(l => l.Id == lineId, ct);
            line?.Uncommit(sack.Pieces);
        }

        var was = sack.OrderName ?? "warehouse";
        sack.Unassign();
        activity.Record(ActivityArea.Sku, "unassign", $"{sack.Code} unassigned from {was}", sack.Id);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

// ---------- Ship / loose dispatch ----------

/// <summary>Ships sacks already assigned to an order. This is where SKU stock actually leaves:
/// each sack's pieces come off OnHand and InSacks together.</summary>
public sealed record ShipSacksCommand(IReadOnlyList<Guid> SackIds) : ICommand<ErrorOr<int>>;

public sealed class ShipSacksValidator : AbstractValidator<ShipSacksCommand>
{
    public ShipSacksValidator() => RuleFor(x => x.SackIds).NotEmpty().Must(i => i.Count <= 500);
}

public sealed class ShipSacksHandler(IAppDbContext db, StockLedger ledger, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<ShipSacksCommand, ErrorOr<int>>
{
    public async ValueTask<ErrorOr<int>> Handle(ShipSacksCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<int>>(async token =>
        {
            var ids = command.SackIds.Distinct().ToList();
            var sacks = await db.Sacks.Include(s => s.Components).Where(s => ids.Contains(s.Id)).ToListAsync(token);
            if (sacks.Count != ids.Count)
                return Error.NotFound(description: "Some sacks were not found — reload and try again.");

            var balanceIds = sacks.SelectMany(s => s.Components).Select(c => c.StockBalanceId).Distinct().ToList();
            var balances = await db.StockBalances.AsNoTracking().Where(b => balanceIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, token);
            var now = clock.UtcNow;
            var pieces = 0;

            foreach (var sack in sacks)
            {
                sack.MarkShipped(now);
                var dispatch = new SkuDispatch(SkuDispatchKind.Shipped, sack.ItemName, null, sack.OrderName, sack.ClientOrderId, sack.OrderName, sack.Code, now);
                foreach (var component in sack.Components.Where(c => c.Quantity > 0))
                {
                    await ledger.ShipFromSackAsync(balances[component.StockBalanceId], component.Quantity, dispatch.Id, sack.Code, token);
                    dispatch.AddComponent(component.StockBalanceId, component.LotSerial, component.Grade, component.Quantity);
                }

                db.SkuDispatches.Add(dispatch);
                pieces += dispatch.Quantity;
            }

            activity.Record(ActivityArea.Sku, "ship", $"Shipped {sacks.Count} sack{(sacks.Count > 1 ? "s" : "")} ({pieces:N0} pcs): {string.Join(", ", sacks.Select(s => s.Code))}");
            await db.SaveChangesAsync(token);
            return pieces;
        }, ct);

        if (!result.IsError)
        {
            await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);
            await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        }

        return result;
    }
}

public sealed record DispatchLooseCommand(string ItemName, Grade Grade, int Quantity, string? Reference) : ICommand<ErrorOr<Success>>;

public sealed class DispatchLooseValidator : AbstractValidator<DispatchLooseCommand>
{
    public DispatchLooseValidator()
    {
        RuleFor(x => x.ItemName).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.Reference).MaximumLength(120);
    }
}

public sealed class DispatchLooseHandler(IAppDbContext db, StockLedger ledger, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<DispatchLooseCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DispatchLooseCommand command, CancellationToken ct)
    {
        await db.InTransactionAsync(async token =>
        {
            var draws = await ledger.PlanFifoAsync(command.ItemName, command.Grade, command.Quantity, token);
            var dispatch = new SkuDispatch(SkuDispatchKind.Loose, draws[0].Balance.ItemName, command.Grade, command.Reference, null, null, null, clock.UtcNow);
            foreach (var draw in draws)
            {
                await ledger.TakeLooseAsync(draw.Balance, draw.Quantity, dispatch.Id, command.Reference, token);
                dispatch.AddComponent(draw.Balance.Id, draw.Balance.LotSerial, draw.Balance.Grade, draw.Quantity);
            }

            db.SkuDispatches.Add(dispatch);
            activity.Record(ActivityArea.Sku, "dispatch", $"Dispatched {command.Quantity} loose {dispatch.ItemName} {command.Grade}{(command.Reference is null ? "" : $" · {command.Reference}")}");
            await db.SaveChangesAsync(token);
            return true;
        }, ct);

        await realtime.PublishAsync(RealtimeTopics.Sku, RealtimeAudience.All, null, ct);
        await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}
