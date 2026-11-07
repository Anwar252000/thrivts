using System.Text.RegularExpressions;
using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Inventory;

namespace Thrivts.Application.Features.Inventory;

public sealed record DispatchLineInput(Guid StockBalanceId, int Quantity, string? Notes);

public sealed record DispatchLineDto(Guid Id, Guid StockBalanceId, string LotSerial, string ItemName, Grade Grade, int Quantity, string? Notes);

public sealed record DispatchDto(
    Guid Id, string Number, DispatchType Type, DateOnly DispatchDate, DateOnly? OrderReceivedDate, Guid? ClientOrderId,
    string? ClientName, string? DestinationCountry, string? ContainerNumber, int? TotalBoxes, decimal? TotalWeightKg,
    string? Notes, int TotalPieces, DateTimeOffset CreatedAt, IReadOnlyList<DispatchLineDto> Lines);

public sealed record SaveDispatchCommand(
    Guid? Id, DispatchType Type, string? Number, DateOnly DispatchDate, DateOnly? OrderReceivedDate, Guid? ClientOrderId,
    string? ClientName, string? DestinationCountry, string? ContainerNumber, int? TotalBoxes, decimal? TotalWeightKg,
    string? Notes, IReadOnlyList<DispatchLineInput> Lines) : ICommand<ErrorOr<DispatchDto>>;

public sealed partial class SaveDispatchValidator : AbstractValidator<SaveDispatchCommand>
{
    public SaveDispatchValidator()
    {
        RuleFor(x => x.Number).Must(n => n is null || NumberPattern().IsMatch(n.Trim()))
            .WithMessage("Order number may only use letters, digits and dashes (2–20).");
        RuleFor(x => x.ClientName).MaximumLength(80);
        RuleFor(x => x.DestinationCountry).MaximumLength(60);
        RuleFor(x => x.ContainerNumber).MaximumLength(60);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.TotalBoxes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalWeightKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item with pieces.")
            .Must(l => l.Count <= Dispatch.MaxLines);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.StockBalanceId).NotEmpty();
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 1_000_000);
            line.RuleFor(l => l.Notes).MaximumLength(300);
        });
    }

    [GeneratedRegex("^[A-Za-z0-9-]{2,20}$")]
    private static partial Regex NumberPattern();
}

/// <summary>
/// Creates or replaces a container (VEWC) / small-order (VEWO) dispatch. Every line draws from a
/// specific lot's stock through the ledger, so nothing can be shipped that isn't physically there —
/// and the same pieces can't also be packed into a sack. Editing reverses the old lines and applies
/// the new ones in one transaction.
/// </summary>
public sealed class SaveDispatchHandler(IAppDbContext db, ISerialGenerator serials, StockLedger ledger, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<SaveDispatchCommand, ErrorOr<DispatchDto>>
{
    public async ValueTask<ErrorOr<DispatchDto>> Handle(SaveDispatchCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Guid>>(async token =>
        {
            Dispatch dispatch;
            if (command.Id is { } id)
            {
                var existing = await db.Dispatches.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id, token);
                if (existing is null)
                    return Error.NotFound(description: "Dispatch not found.");
                if (existing.Type != command.Type)
                    return Error.Validation(description: "A dispatch cannot change between container and small order.");

                await ReverseLinesAsync(existing, token);
                existing.ClearLines();
                dispatch = existing;
            }
            else
            {
                var number = await ResolveNumberAsync(command, token);
                if (number.IsError)
                    return number.Errors;

                dispatch = new Dispatch(number.Value, command.Type);
                db.Dispatches.Add(dispatch);
            }

            dispatch.SetDetails(command.DispatchDate, command.OrderReceivedDate, command.ClientOrderId, command.ClientName?.Trim(),
                command.DestinationCountry?.Trim(), command.ContainerNumber?.Trim(), command.TotalBoxes, command.TotalWeightKg, command.Notes?.Trim());

            var merged = command.Lines.GroupBy(l => l.StockBalanceId)
                .Select(g => new DispatchLineInput(g.Key, g.Sum(l => l.Quantity), string.Join("; ", g.Select(l => l.Notes).Where(n => !string.IsNullOrWhiteSpace(n)))))
                .ToList();
            var balanceIds = merged.Select(l => l.StockBalanceId).ToList();
            var balances = await (
                    from b in db.StockBalances.AsNoTracking()
                    join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                    where balanceIds.Contains(b.Id)
                    select b)
                .ToDictionaryAsync(b => b.Id, token);

            foreach (var line in merged)
            {
                if (!balances.TryGetValue(line.StockBalanceId, out var balance))
                    return Error.NotFound(description: "One of the lines refers to stock that no longer exists — reload and try again.");

                await ledger.TakeForDispatchAsync(balance, line.Quantity, dispatch.Type, dispatch.Id, dispatch.Number, command.DispatchDate, token);
                dispatch.AddLine(balance, line.Quantity, string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes);
            }

            activity.Record(ActivityArea.Dispatch, command.Id is null ? "dispatched" : "dispatch-edited",
                $"{dispatch.Number} — {dispatch.TotalPieces:N0} pcs{(dispatch.ClientName is null ? "" : $" to {dispatch.ClientName}")}", dispatch.Id);
            await db.SaveChangesAsync(token);
            return dispatch.Id;
        }, ct);

        if (result.IsError)
            return result.Errors;

        await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        return (await new GetDispatchHandler(db).Handle(new GetDispatchQuery(result.Value), ct)).Value;
    }

    private async Task ReverseLinesAsync(Dispatch dispatch, CancellationToken ct)
    {
        var balanceIds = dispatch.Lines.Select(l => l.StockBalanceId).Distinct().ToList();
        var balances = await db.StockBalances.AsNoTracking().Where(b => balanceIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, ct);
        foreach (var line in dispatch.Lines)
            await ledger.RestoreDispatchAsync(balances[line.StockBalanceId], line.Quantity, dispatch.Type, dispatch.Id, dispatch.Number, ct);
    }

    private async Task<ErrorOr<string>> ResolveNumberAsync(SaveDispatchCommand command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.Number))
        {
            var typed = command.Number.Trim().ToUpperInvariant();
            if (await db.Dispatches.IgnoreQueryFilters().AnyAsync(d => d.Number == typed, ct))
                return Error.Conflict(description: $"{typed} is already used — leave the number blank to get the next one.");
            return typed;
        }

        var (counter, prefix) = command.Type == DispatchType.Container
            ? (SerialCounters.ContainerDispatch, "VEWC")
            : (SerialCounters.SmallDispatch, "VEWO");

        return $"{prefix}{await serials.NextAsync(counter, ct):00}";
    }
}

public sealed record DeleteDispatchCommand(Guid Id) : ICommand<ErrorOr<Success>>;

/// <summary>Puts every line's pieces back into stock, then soft-deletes the dispatch.</summary>
public sealed class DeleteDispatchHandler(IAppDbContext db, StockLedger ledger, ICurrentUser user, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<DeleteDispatchCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteDispatchCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var dispatch = await db.Dispatches.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == command.Id, token);
            if (dispatch is null)
                return Error.NotFound(description: "Dispatch not found.");

            var balanceIds = dispatch.Lines.Select(l => l.StockBalanceId).Distinct().ToList();
            var balances = await db.StockBalances.AsNoTracking().Where(b => balanceIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, token);
            foreach (var line in dispatch.Lines)
                await ledger.RestoreDispatchAsync(balances[line.StockBalanceId], line.Quantity, dispatch.Type, dispatch.Id, dispatch.Number, token);

            dispatch.MarkDeleted(clock.UtcNow, user.UserId);
            activity.Record(ActivityArea.Dispatch, "dispatch-deleted", $"{dispatch.Number} deleted — {dispatch.TotalPieces:N0} pcs back in stock", dispatch.Id);
            await db.SaveChangesAsync(token);
            return Result.Success;
        }, ct);

        if (!result.IsError)
            await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);

        return result;
    }
}

public sealed record GetDispatchQuery(Guid Id) : IQuery<ErrorOr<DispatchDto>>;

public sealed class GetDispatchHandler(IAppDbContext db) : IQueryHandler<GetDispatchQuery, ErrorOr<DispatchDto>>
{
    public async ValueTask<ErrorOr<DispatchDto>> Handle(GetDispatchQuery query, CancellationToken ct)
    {
        var dispatch = await db.Dispatches.AsNoTracking()
            .Where(d => d.Id == query.Id)
            .Select(DispatchProjection.ToDto)
            .FirstOrDefaultAsync(ct);

        return dispatch is null ? Error.NotFound(description: "Dispatch not found.") : dispatch;
    }
}

public sealed record GetDispatchesQuery(DispatchType? Type, string? Cursor, int? Size) : IQuery<CursorPage<DispatchDto>>;

public sealed class GetDispatchesHandler(IAppDbContext db) : IQueryHandler<GetDispatchesQuery, CursorPage<DispatchDto>>
{
    public async ValueTask<CursorPage<DispatchDto>> Handle(GetDispatchesQuery query, CancellationToken ct)
    {
        var size = CursorPage<DispatchDto>.NormalizeSize(query.Size);
        var dispatches = db.Dispatches.AsNoTracking();
        if (query.Type is { } type)
            dispatches = dispatches.Where(d => d.Type == type);
        if (Cursor.Decode(query.Cursor) is { } c)
            dispatches = dispatches.Where(d => d.CreatedAt < c.At || (d.CreatedAt == c.At && d.Id.CompareTo(c.Id) < 0));

        var page = await dispatches.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id).Take(size)
            .Select(DispatchProjection.ToDto).ToListAsync(ct);
        var next = page.Count == size ? new Cursor(page[^1].CreatedAt, page[^1].Id).Encode() : null;
        return new CursorPage<DispatchDto>(page, next);
    }
}

internal static class DispatchProjection
{
    public static readonly System.Linq.Expressions.Expression<Func<Dispatch, DispatchDto>> ToDto = d => new DispatchDto(
        d.Id, d.Number, d.Type, d.DispatchDate, d.OrderReceivedDate, d.ClientOrderId, d.ClientName, d.DestinationCountry,
        d.ContainerNumber, d.TotalBoxes, d.TotalWeightKg, d.Notes, d.TotalPieces, d.CreatedAt,
        d.Lines.Select(l => new DispatchLineDto(l.Id, l.StockBalanceId, l.LotSerial, l.ItemName, l.Grade, l.Quantity, l.Notes)).ToList());
}
