using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Gate;

public enum GateDirection
{
    In,
    Out,
}

public sealed record GateEntryDto(Guid Id, GateDirection Direction, string Serial, Guid SupplierId, string SupplierName, int Sacks, DateOnly Date, DateTimeOffset CreatedAt);

public static class GateSerials
{
    public static string Lot(long n) => $"VEWL{n:00}";

    public static string Exit(long n) => $"GATE-{n:000}";
}

public sealed record RecordGateEntryCommand(GateDirection Direction, Guid SupplierId, int Sacks, DateOnly? Date) : ICommand<ErrorOr<GateEntryDto>>;

public sealed class RecordGateEntryValidator : AbstractValidator<RecordGateEntryCommand>
{
    public RecordGateEntryValidator(IClock clock)
    {
        RuleFor(x => x.SupplierId).NotEmpty().WithMessage("Tap the supplier first.");
        RuleFor(x => x.Sacks).InclusiveBetween(1, 10_000).WithMessage("Enter the number of sacks.");
        RuleFor(x => x.Date)
            .Must(d => d is null || (d.Value <= clock.Today.AddDays(1) && d.Value >= clock.Today.AddYears(-1)))
            .WithMessage("Choose a date within the last year.");
    }
}

/// <summary>Arrivals become VEWL lots for the supervisor to count; stock-outs (rejected sacks going
/// back) get a GATE number. Back-dating is allowed — the guard picks the business date.</summary>
public sealed class RecordGateEntryHandler(IAppDbContext db, ISerialGenerator serials, ICurrentUser user, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<RecordGateEntryCommand, ErrorOr<GateEntryDto>>
{
    public async ValueTask<ErrorOr<GateEntryDto>> Handle(RecordGateEntryCommand command, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == command.SupplierId && !s.IsArchived, ct);
        if (supplier is null)
            return Error.NotFound(description: "Supplier not found.");

        var date = command.Date ?? clock.Today;
        var dto = await db.InTransactionAsync(async token =>
        {
            if (command.Direction == GateDirection.In)
            {
                var serial = GateSerials.Lot(await serials.NextAsync(SerialCounters.Lot, token));
                var lot = new Lot(serial, supplier.Id, supplier.Name, command.Sacks, date, user.RequireUserId(), clock.UtcNow);
                db.Lots.Add(lot);
                activity.Record(ActivityArea.Gate, "arrival", $"{serial} — {command.Sacks} sacks from {supplier.Name}", lot.Id);
                await db.SaveChangesAsync(token);
                return new GateEntryDto(lot.Id, GateDirection.In, serial, supplier.Id, supplier.Name, lot.Sacks, date, lot.CreatedAt);
            }

            var exitSerial = GateSerials.Exit(await serials.NextAsync(SerialCounters.GateExit, token));
            var exit = new GateExit(exitSerial, supplier.Id, supplier.Name, command.Sacks, date);
            db.GateExits.Add(exit);
            activity.Record(ActivityArea.Gate, "exit", $"{exitSerial} — {command.Sacks} sacks out to {supplier.Name}", exit.Id);
            await db.SaveChangesAsync(token);
            return new GateEntryDto(exit.Id, GateDirection.Out, exitSerial, supplier.Id, supplier.Name, exit.Sacks, date, exit.CreatedAt);
        }, ct);

        await realtime.PublishAsync(RealtimeTopics.Lots, RealtimeAudience.All, null, ct);
        return dto;
    }
}

public sealed record NextGateSerialsDto(string NextLot, string NextExit);

/// <summary>A preview only — the real number is taken when the entry is saved, so two guards can
/// both see "VEWL12" but will receive VEWL12 and VEWL13.</summary>
public sealed record GetNextGateSerialsQuery : IQuery<NextGateSerialsDto>;

public sealed class GetNextGateSerialsHandler(ISerialGenerator serials) : IQueryHandler<GetNextGateSerialsQuery, NextGateSerialsDto>
{
    public async ValueTask<NextGateSerialsDto> Handle(GetNextGateSerialsQuery query, CancellationToken ct) =>
        new(GateSerials.Lot(await serials.PeekAsync(SerialCounters.Lot, ct)), GateSerials.Exit(await serials.PeekAsync(SerialCounters.GateExit, ct)));
}

public sealed record GetGateHistoryQuery(string? Cursor, int? Size) : IQuery<CursorPage<GateEntryDto>>;

/// <summary>Arrivals and stock-outs, newest first, keyset-paged across both tables.</summary>
public sealed class GetGateHistoryHandler(IAppDbContext db) : IQueryHandler<GetGateHistoryQuery, CursorPage<GateEntryDto>>
{
    public async ValueTask<CursorPage<GateEntryDto>> Handle(GetGateHistoryQuery query, CancellationToken ct)
    {
        var size = CursorPage<GateEntryDto>.NormalizeSize(query.Size);
        var cursor = Cursor.Decode(query.Cursor);

        var lots = db.Lots.AsNoTracking();
        var exits = db.GateExits.AsNoTracking();
        if (cursor is { } c)
        {
            lots = lots.Where(l => l.CreatedAt < c.At || (l.CreatedAt == c.At && l.Id.CompareTo(c.Id) < 0));
            exits = exits.Where(e => e.CreatedAt < c.At || (e.CreatedAt == c.At && e.Id.CompareTo(c.Id) < 0));
        }

        var ins = await lots.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id).Take(size)
            .Select(l => new GateEntryDto(l.Id, GateDirection.In, l.Serial, l.SupplierId, l.SupplierName, l.Sacks, l.ArrivalDate, l.CreatedAt))
            .ToListAsync(ct);
        var outs = await exits.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).Take(size)
            .Select(e => new GateEntryDto(e.Id, GateDirection.Out, e.Serial, e.SupplierId, e.SupplierName, e.Sacks, e.BusinessDate, e.CreatedAt))
            .ToListAsync(ct);

        var page = ins.Concat(outs).OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).Take(size).ToList();
        var next = page.Count == size ? new Cursor(page[^1].CreatedAt, page[^1].Id).Encode() : null;
        return new CursorPage<GateEntryDto>(page, next);
    }
}
