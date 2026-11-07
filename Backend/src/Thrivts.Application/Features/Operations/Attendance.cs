using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Operations;

namespace Thrivts.Application.Features.Operations;

public sealed record AttendanceRowDto(Guid LabourerId, string Name, bool Present, TimeOnly? TimeIn, TimeOnly? TimeOut, decimal OvertimeHours);

public sealed record AttendanceDayDto(DateOnly Date, bool IsSunday, IReadOnlyList<AttendanceRowDto> Rows);

public sealed record GetAttendanceDayQuery(DateOnly? Date) : IQuery<AttendanceDayDto>;

public sealed class GetAttendanceDayHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetAttendanceDayQuery, AttendanceDayDto>
{
    public async ValueTask<AttendanceDayDto> Handle(GetAttendanceDayQuery query, CancellationToken ct)
    {
        var date = query.Date ?? clock.Today;
        var records = await db.AttendanceRecords.AsNoTracking().Where(a => a.BusinessDate == date).ToDictionaryAsync(a => a.LabourerId, ct);
        var recordedIds = records.Keys.ToList();
        var labourers = await db.Labourers.AsNoTracking()
            .Where(l => l.IsActive || recordedIds.Contains(l.Id))
            .OrderBy(l => l.CreatedAt)
            .Select(l => new { l.Id, l.Name })
            .ToListAsync(ct);

        var rows = labourers.Select(l => records.TryGetValue(l.Id, out var r)
            ? new AttendanceRowDto(l.Id, l.Name, true, r.TimeIn, r.TimeOut, r.OvertimeHours)
            : new AttendanceRowDto(l.Id, l.Name, false, null, null, 0)).ToList();

        return new AttendanceDayDto(date, date.DayOfWeek == DayOfWeek.Sunday, rows);
    }
}

/// <summary>Ticking "present" creates the day's record (9–6 by default); unticking removes it.</summary>
public sealed record SetAttendanceCommand(DateOnly Date, Guid LabourerId, bool Present, TimeOnly? TimeIn, TimeOnly? TimeOut) : ICommand<ErrorOr<AttendanceRowDto>>;

public sealed class SetAttendanceValidator : AbstractValidator<SetAttendanceCommand>
{
    public SetAttendanceValidator(IClock clock) =>
        RuleFor(x => x.Date).LessThanOrEqualTo(_ => clock.Today.AddDays(1)).WithMessage("Attendance can't be filled for future days.");
}

public sealed class SetAttendanceHandler(IAppDbContext db) : ICommandHandler<SetAttendanceCommand, ErrorOr<AttendanceRowDto>>
{
    public async ValueTask<ErrorOr<AttendanceRowDto>> Handle(SetAttendanceCommand command, CancellationToken ct)
    {
        var labourer = await db.Labourers.AsNoTracking().FirstOrDefaultAsync(l => l.Id == command.LabourerId, ct);
        if (labourer is null)
            return Error.NotFound(description: "Worker not found.");

        var record = await db.AttendanceRecords.FirstOrDefaultAsync(a => a.BusinessDate == command.Date && a.LabourerId == command.LabourerId, ct);
        if (!command.Present)
        {
            if (record is not null)
                db.AttendanceRecords.Remove(record);
            await db.SaveChangesAsync(ct);
            return new AttendanceRowDto(labourer.Id, labourer.Name, false, null, null, 0);
        }

        if (record is null)
        {
            record = new AttendanceRecord(command.Date, labourer.Id, labourer.Name);
            db.AttendanceRecords.Add(record);
        }

        record.SetTimes(command.TimeIn ?? record.TimeIn, command.TimeOut ?? record.TimeOut);
        await db.SaveChangesAsync(ct);
        return new AttendanceRowDto(labourer.Id, labourer.Name, true, record.TimeIn, record.TimeOut, record.OvertimeHours);
    }
}

public sealed record MarkEveryonePresentCommand(DateOnly Date) : ICommand<Success>;

public sealed class MarkEveryonePresentHandler(IAppDbContext db) : ICommandHandler<MarkEveryonePresentCommand, Success>
{
    public async ValueTask<Success> Handle(MarkEveryonePresentCommand command, CancellationToken ct)
    {
        var present = await db.AttendanceRecords.Where(a => a.BusinessDate == command.Date).Select(a => a.LabourerId).ToListAsync(ct);
        var missing = await db.Labourers.AsNoTracking().Where(l => l.IsActive && !present.Contains(l.Id)).ToListAsync(ct);
        foreach (var labourer in missing)
            db.AttendanceRecords.Add(new AttendanceRecord(command.Date, labourer.Id, labourer.Name));

        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

public enum AttendanceLogStatus
{
    Logged,
    Missing,
    Off,
}

public sealed record AttendanceLogDay(DateOnly Date, AttendanceLogStatus Status, int Present);

public sealed record AttendanceLogHistoryDto(int LoggedDays, int MissingDays, int Percent, bool LoggedToday, IReadOnlyList<AttendanceLogDay> Days);

/// <summary>The supervisor's dashboard: on which recent workdays attendance was filled in. Sundays
/// are off and never count as missed; today counts as missed only once it's over.</summary>
public sealed record GetAttendanceLogHistoryQuery(int Days = 30) : IQuery<AttendanceLogHistoryDto>;

public sealed class GetAttendanceLogHistoryHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetAttendanceLogHistoryQuery, AttendanceLogHistoryDto>
{
    public async ValueTask<AttendanceLogHistoryDto> Handle(GetAttendanceLogHistoryQuery query, CancellationToken ct)
    {
        var today = clock.Today;
        var from = today.AddDays(-(Math.Clamp(query.Days, 1, 90) - 1));
        var present = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.BusinessDate >= from && a.BusinessDate <= today)
            .GroupBy(a => a.BusinessDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

        var days = new List<AttendanceLogDay>();
        for (var d = from; d <= today; d = d.AddDays(1))
        {
            var count = present.GetValueOrDefault(d);
            var status = count > 0 ? AttendanceLogStatus.Logged
                : d.DayOfWeek == DayOfWeek.Sunday || d == today ? AttendanceLogStatus.Off
                : AttendanceLogStatus.Missing;
            days.Add(new AttendanceLogDay(d, status, count));
        }

        var logged = days.Count(d => d.Status == AttendanceLogStatus.Logged);
        var missing = days.Count(d => d.Status == AttendanceLogStatus.Missing);
        var percent = logged + missing == 0 ? 0 : (int)Math.Round(logged * 100.0 / (logged + missing));
        return new AttendanceLogHistoryDto(logged, missing, percent, present.ContainsKey(today), days);
    }
}

public sealed record MonthlyOvertimeRow(string Name, int DaysWorked, int Sundays, decimal OvertimeHours);

public sealed record GetMonthlyOvertimeQuery(int Year, int Month) : IQuery<IReadOnlyList<MonthlyOvertimeRow>>;

public sealed class GetMonthlyOvertimeHandler(IAppDbContext db) : IQueryHandler<GetMonthlyOvertimeQuery, IReadOnlyList<MonthlyOvertimeRow>>
{
    public async ValueTask<IReadOnlyList<MonthlyOvertimeRow>> Handle(GetMonthlyOvertimeQuery query, CancellationToken ct)
    {
        var from = new DateOnly(query.Year, Math.Clamp(query.Month, 1, 12), 1);
        var to = from.AddMonths(1);
        return await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.BusinessDate >= from && a.BusinessDate < to)
            .GroupBy(a => a.LabourerName)
            .Select(g => new MonthlyOvertimeRow(g.Key, g.Count(), g.Count(a => a.IsSunday), g.Sum(a => a.OvertimeHours)))
            .OrderByDescending(r => r.OvertimeHours)
            .ToListAsync(ct);
    }
}

public sealed record AttendanceExportRow(DateOnly Date, string Day, string Worker, TimeOnly TimeIn, TimeOnly TimeOut, decimal OvertimeHours, bool Sunday);

public sealed record ExportAttendanceQuery(int Year, int Month) : IStreamQuery<AttendanceExportRow>;

public sealed class ExportAttendanceHandler(IAppDbContext db) : IStreamQueryHandler<ExportAttendanceQuery, AttendanceExportRow>
{
    public async IAsyncEnumerable<AttendanceExportRow> Handle(ExportAttendanceQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var from = new DateOnly(query.Year, Math.Clamp(query.Month, 1, 12), 1);
        var to = from.AddMonths(1);
        var rows = db.AttendanceRecords.AsNoTracking()
            .Where(a => a.BusinessDate >= from && a.BusinessDate < to)
            .OrderBy(a => a.BusinessDate).ThenBy(a => a.LabourerName)
            .AsAsyncEnumerable();

        await foreach (var a in rows.WithCancellation(ct))
            yield return new AttendanceExportRow(a.BusinessDate, a.BusinessDate.DayOfWeek.ToString(), a.LabourerName, a.TimeIn, a.TimeOut, a.OvertimeHours, a.IsSunday);
    }
}
