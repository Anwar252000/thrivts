using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Operations;

namespace Thrivts.Application.Features.Operations;

public sealed record ExpenseDto(Guid Id, DateOnly Date, string Description, ExpenseCategory Category, decimal Amount, bool HasReceipt, string? ReceiptLink);

public sealed record BlockerDto(Guid Id, DateOnly Date, string Description, Department Department, string? Responsible, bool NeedsOwner, bool Escalated);

public sealed record DaySummaryDto(DayRating Rating, OnTrackStatus? OnTrack, string? FocusTomorrow, string? OwnerActions, string? TeamReport);

public sealed record TeamCheckDto(string PersonName, string RoleLabel, AttendanceMark Attended, Punctuality Punctuality, EntryStatus EntrySubmitted, bool HasIssues, string? Notes);

public sealed record SubmissionDto(DailySection Section, bool IsNoProgress, DateTimeOffset SubmittedAt);

public sealed record DaySheetDto(
    DateOnly Date, IReadOnlyList<ExpenseDto> Expenses, decimal ExpenseTotal, int MissingReceipts,
    IReadOnlyList<BlockerDto> Blockers, DaySummaryDto Summary, IReadOnlyList<TeamCheckDto> TeamChecks, IReadOnlyList<SubmissionDto> Submissions);

/// <summary>Everything the GM fills in for one business date, loaded in one round-trip. Opening a
/// past date shows what was saved then (the legacy forms always opened blank).</summary>
public sealed record GetDaySheetQuery(DateOnly? Date) : IQuery<DaySheetDto>;

public sealed class GetDaySheetHandler(IAppDbContext db, IClock clock, ICurrentUser user) : IQueryHandler<GetDaySheetQuery, DaySheetDto>
{
    public async ValueTask<DaySheetDto> Handle(GetDaySheetQuery query, CancellationToken ct)
    {
        var date = query.Date ?? clock.Today;
        var userId = user.RequireUserId();

        var expenses = await db.Expenses.AsNoTracking().Where(e => e.BusinessDate == date).OrderBy(e => e.CreatedAt)
            .Select(e => new ExpenseDto(e.Id, e.BusinessDate, e.Description, e.Category, e.Amount, e.HasReceipt, e.ReceiptLink))
            .ToListAsync(ct);
        var blockers = await db.Blockers.AsNoTracking().Where(b => b.BusinessDate == date).OrderBy(b => b.CreatedAt)
            .Select(b => new BlockerDto(b.Id, b.BusinessDate, b.Description, b.Department, b.Responsible, b.NeedsOwner, b.EscalationId != null))
            .ToListAsync(ct);
        var summary = await db.DaySummaries.AsNoTracking().Where(s => s.BusinessDate == date)
            .Select(s => new DaySummaryDto(s.Rating, s.OnTrack, s.FocusTomorrow, s.OwnerActions, s.TeamReport))
            .FirstOrDefaultAsync(ct) ?? new DaySummaryDto(DayRating.Okay, null, null, null, null);
        var team = await db.TeamChecks.AsNoTracking().Where(t => t.BusinessDate == date).OrderBy(t => t.CreatedAt)
            .Select(t => new TeamCheckDto(t.PersonName, t.RoleLabel, t.Attended, t.Punctuality, t.EntrySubmitted, t.HasIssues, t.Notes))
            .ToListAsync(ct);
        var submissions = await db.DailySubmissions.AsNoTracking().Where(s => s.UserId == userId && s.BusinessDate == date)
            .Select(s => new SubmissionDto(s.Section, s.IsNoProgress, s.SubmittedAt))
            .ToListAsync(ct);

        return new DaySheetDto(date, expenses, expenses.Sum(e => e.Amount), expenses.Count(e => e.Amount > 0 && !e.HasReceipt),
            blockers, summary, team, submissions);
    }
}

// ---------- Expenses (row by row — never "delete the day and re-insert") ----------

public sealed record SaveExpenseCommand(Guid? Id, DateOnly Date, string Description, ExpenseCategory Category, decimal Amount, bool HasReceipt, string? ReceiptLink)
    : ICommand<ErrorOr<ExpenseDto>>;

public sealed class SaveExpenseValidator : AbstractValidator<SaveExpenseCommand>
{
    public SaveExpenseValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).InclusiveBetween(0, 100_000_000);
        RuleFor(x => x.ReceiptLink).MaximumLength(500)
            .Must(l => string.IsNullOrWhiteSpace(l) || (Uri.TryCreate(l, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp)))
            .WithMessage("Receipt link must be a web address (https://...).");
    }
}

public sealed class SaveExpenseHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<SaveExpenseCommand, ErrorOr<ExpenseDto>>
{
    public async ValueTask<ErrorOr<ExpenseDto>> Handle(SaveExpenseCommand command, CancellationToken ct)
    {
        Expense expense;
        if (command.Id is { } id)
        {
            var existing = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (existing is null)
                return Error.NotFound(description: "Expense not found.");
            existing.Update(command.Description, command.Category, command.Amount, command.HasReceipt, command.ReceiptLink);
            expense = existing;
        }
        else
        {
            expense = new Expense(command.Date, command.Description, command.Category, command.Amount, command.HasReceipt, command.ReceiptLink);
            db.Expenses.Add(expense);
        }

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Operations, RealtimeAudience.All, null, ct);
        return new ExpenseDto(expense.Id, expense.BusinessDate, expense.Description, expense.Category, expense.Amount, expense.HasReceipt, expense.ReceiptLink);
    }
}

public sealed record DeleteExpenseCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteExpenseHandler(IAppDbContext db) : ICommandHandler<DeleteExpenseCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteExpenseCommand command, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == command.Id, ct);
        if (expense is null)
            return Error.NotFound(description: "Expense not found.");

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

// ---------- Blockers ----------

public sealed record SaveBlockerCommand(Guid? Id, DateOnly Date, string Description, Department Department, string? Responsible, bool NeedsOwner) : ICommand<ErrorOr<BlockerDto>>;

public sealed class SaveBlockerValidator : AbstractValidator<SaveBlockerCommand>
{
    public SaveBlockerValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Responsible).MaximumLength(80);
    }
}

/// <summary>A blocker marked "needs Afnan" opens an escalation to the owner once — editing it later
/// never creates a duplicate.</summary>
public sealed class SaveBlockerHandler(IAppDbContext db, ICurrentUser user, IRealtimeNotifier realtime) : ICommandHandler<SaveBlockerCommand, ErrorOr<BlockerDto>>
{
    public async ValueTask<ErrorOr<BlockerDto>> Handle(SaveBlockerCommand command, CancellationToken ct)
    {
        Blocker blocker;
        if (command.Id is { } id)
        {
            var existing = await db.Blockers.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (existing is null)
                return Error.NotFound(description: "Blocker not found.");
            existing.Update(command.Description, command.Department, command.Responsible, command.NeedsOwner);
            blocker = existing;
        }
        else
        {
            blocker = new Blocker(command.Date, command.Description, command.Department, command.Responsible, command.NeedsOwner);
            db.Blockers.Add(blocker);
        }

        var escalated = false;
        if (blocker.NeedsOwner && blocker.EscalationId is null)
        {
            var escalation = new Escalation(user.RequireUserId(), user.Name ?? "Staff", EscalationSource.Blocker, blocker.Description, blocker.Department.ToString(), blocker.BusinessDate);
            db.Escalations.Add(escalation);
            blocker.LinkEscalation(escalation.Id);
            escalated = true;
        }

        await db.SaveChangesAsync(ct);
        if (escalated)
            await realtime.PublishAsync(RealtimeTopics.Escalations, RealtimeAudience.Owner, null, ct);

        return new BlockerDto(blocker.Id, blocker.BusinessDate, blocker.Description, blocker.Department, blocker.Responsible, blocker.NeedsOwner, blocker.EscalationId != null);
    }
}

public sealed record DeleteBlockerCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteBlockerHandler(IAppDbContext db) : ICommandHandler<DeleteBlockerCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteBlockerCommand command, CancellationToken ct)
    {
        var blocker = await db.Blockers.FirstOrDefaultAsync(b => b.Id == command.Id, ct);
        if (blocker is null)
            return Error.NotFound(description: "Blocker not found.");

        db.Blockers.Remove(blocker);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

// ---------- Day summary / team check ----------

public sealed record SaveDaySummaryCommand(DateOnly Date, DayRating Rating, OnTrackStatus? OnTrack, string? FocusTomorrow, string? OwnerActions) : ICommand<Success>;

public sealed class SaveDaySummaryValidator : AbstractValidator<SaveDaySummaryCommand>
{
    public SaveDaySummaryValidator()
    {
        RuleFor(x => x.FocusTomorrow).MaximumLength(500);
        RuleFor(x => x.OwnerActions).MaximumLength(2000);
    }
}

public sealed class SaveDaySummaryHandler(IAppDbContext db) : ICommandHandler<SaveDaySummaryCommand, Success>
{
    public async ValueTask<Success> Handle(SaveDaySummaryCommand command, CancellationToken ct)
    {
        var summary = await db.DaySummaries.FirstOrDefaultAsync(s => s.BusinessDate == command.Date, ct);
        if (summary is null)
        {
            summary = new DaySummary(command.Date);
            db.DaySummaries.Add(summary);
        }

        summary.Update(command.Rating, command.OnTrack, command.FocusTomorrow?.Trim(), command.OwnerActions?.Trim());
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

public sealed record TeamCheckInput(string PersonName, string RoleLabel, AttendanceMark Attended, Punctuality Punctuality, EntryStatus EntrySubmitted, bool HasIssues, string? Notes);

public sealed record SaveTeamCheckCommand(DateOnly Date, IReadOnlyList<TeamCheckInput> Rows, string? Report) : ICommand<Success>;

public sealed class SaveTeamCheckValidator : AbstractValidator<SaveTeamCheckCommand>
{
    public SaveTeamCheckValidator()
    {
        RuleFor(x => x.Rows).NotNull().Must(r => r.Count <= 50);
        RuleFor(x => x.Report).MaximumLength(2000);
        RuleForEach(x => x.Rows).ChildRules(r =>
        {
            r.RuleFor(x => x.PersonName).NotEmpty().MaximumLength(60);
            r.RuleFor(x => x.RoleLabel).NotEmpty().MaximumLength(60);
            r.RuleFor(x => x.Notes).MaximumLength(500);
        });
    }
}

public sealed class SaveTeamCheckHandler(IAppDbContext db) : ICommandHandler<SaveTeamCheckCommand, Success>
{
    public async ValueTask<Success> Handle(SaveTeamCheckCommand command, CancellationToken ct)
    {
        var existing = await db.TeamChecks.Where(t => t.BusinessDate == command.Date).ToListAsync(ct);
        foreach (var row in command.Rows)
        {
            var check = existing.FirstOrDefault(t => string.Equals(t.PersonName, row.PersonName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (check is null)
            {
                check = new TeamCheck(command.Date, row.PersonName, row.RoleLabel);
                db.TeamChecks.Add(check);
            }

            check.Update(row.Attended, row.Punctuality, row.EntrySubmitted, row.HasIssues, row.Notes?.Trim());
        }

        var summary = await db.DaySummaries.FirstOrDefaultAsync(s => s.BusinessDate == command.Date, ct);
        if (summary is null)
        {
            summary = new DaySummary(command.Date);
            db.DaySummaries.Add(summary);
        }

        summary.SetTeamReport(command.Report?.Trim());
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

// ---------- Submissions & compliance ----------

public sealed record SubmitSectionCommand(DailySection Section, DateOnly Date, bool NoProgress) : ICommand<Success>;

/// <summary>"Submit &amp; Save" or "No progress today" for one section and date.</summary>
public sealed class SubmitSectionHandler(IAppDbContext db, ICurrentUser user, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<SubmitSectionCommand, Success>
{
    public async ValueTask<Success> Handle(SubmitSectionCommand command, CancellationToken ct)
    {
        var userId = user.RequireUserId();
        var submission = await db.DailySubmissions.FirstOrDefaultAsync(s => s.UserId == userId && s.Section == command.Section && s.BusinessDate == command.Date, ct);
        if (submission is null)
            db.DailySubmissions.Add(new DailySubmission(userId, command.Section, command.Date, command.NoProgress, clock.UtcNow));
        else
            submission.Resubmit(command.NoProgress, clock.UtcNow);

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Operations, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public enum DayStatus
{
    Complete,
    Partial,
    Missing,
    Off,
}

public sealed record ComplianceDay(DateOnly Date, DayStatus Status, IReadOnlyList<DailySection> Submitted);

public sealed record SectionFillRate(DailySection Section, bool Mandatory, int Percent);

public sealed record ComplianceDto(
    int CompleteDays, int PartialDays, int MissingDays, int Streak, int CompliancePercent,
    IReadOnlyList<DailySection> MissingToday, IReadOnlyList<SectionFillRate> FillRates, IReadOnlyList<ComplianceDay> Days);

/// <summary>
/// The GM's 30-day dashboard. A day is complete when every mandatory section (team check,
/// expenditure, blockers) was submitted or marked "no progress"; partial when anything was; Sundays
/// are days off. The streak counts consecutive complete working days back from today (today is
/// skipped while still incomplete).
/// </summary>
public sealed record GetComplianceQuery(int Days) : IQuery<ComplianceDto>;

public sealed class GetComplianceHandler(IAppDbContext db, IClock clock, ICurrentUser user) : IQueryHandler<GetComplianceQuery, ComplianceDto>
{
    public static readonly DailySection[] Mandatory = [DailySection.TeamCheck, DailySection.Expenditure, DailySection.Blockers];
    private static readonly DailySection[] Tracked = [DailySection.Requirements, DailySection.TeamCheck, DailySection.Expenditure, DailySection.Blockers];

    public async ValueTask<ComplianceDto> Handle(GetComplianceQuery query, CancellationToken ct)
    {
        var days = Math.Clamp(query.Days, 7, 180);
        var today = clock.Today;
        var from = today.AddDays(-(days - 1));
        var userId = user.RequireUserId();

        var submissions = await db.DailySubmissions.AsNoTracking()
            .Where(s => s.UserId == userId && s.BusinessDate >= from && s.BusinessDate <= today)
            .Select(s => new { s.BusinessDate, s.Section })
            .ToListAsync(ct);
        var byDay = submissions.GroupBy(s => s.BusinessDate).ToDictionary(g => g.Key, g => g.Select(s => s.Section).ToHashSet());

        var list = new List<ComplianceDay>(days);
        for (var date = today; date >= from; date = date.AddDays(-1))
        {
            var done = byDay.GetValueOrDefault(date) ?? [];
            var status = date.DayOfWeek == DayOfWeek.Sunday ? DayStatus.Off
                : Mandatory.All(done.Contains) ? DayStatus.Complete
                : done.Count > 0 ? DayStatus.Partial
                : DayStatus.Missing;
            list.Add(new ComplianceDay(date, status, done.ToList()));
        }

        var workdays = list.Where(d => d.Status != DayStatus.Off).ToList();
        var streak = 0;
        foreach (var day in workdays)
        {
            if (day.Status == DayStatus.Complete)
                streak++;
            else if (day.Date != today)
                break;
        }

        var fillRates = Tracked.Select(section => new SectionFillRate(section, Mandatory.Contains(section),
            workdays.Count == 0 ? 0 : (int)Math.Round(100.0 * workdays.Count(d => d.Submitted.Contains(section)) / workdays.Count))).ToList();

        var todayDone = byDay.GetValueOrDefault(today) ?? [];
        var complete = workdays.Count(d => d.Status == DayStatus.Complete);
        return new ComplianceDto(
            complete,
            workdays.Count(d => d.Status == DayStatus.Partial),
            workdays.Count(d => d.Status == DayStatus.Missing),
            streak,
            workdays.Count == 0 ? 0 : (int)Math.Round(100.0 * complete / workdays.Count),
            today.DayOfWeek == DayOfWeek.Sunday ? [] : Mandatory.Where(m => !todayDone.Contains(m)).ToList(),
            fillRates,
            list);
    }
}

// ---------- Warehouse zones (inventory manager) ----------

public sealed record ZoneCheckDto(WarehouseZone Zone, ZoneStatus? Status, string? Notes);

public sealed record GetZoneChecksQuery(DateOnly? Date) : IQuery<IReadOnlyList<ZoneCheckDto>>;

public sealed class GetZoneChecksHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetZoneChecksQuery, IReadOnlyList<ZoneCheckDto>>
{
    public async ValueTask<IReadOnlyList<ZoneCheckDto>> Handle(GetZoneChecksQuery query, CancellationToken ct)
    {
        var date = query.Date ?? clock.Today;
        var saved = await db.ZoneChecks.AsNoTracking().Where(z => z.BusinessDate == date).ToDictionaryAsync(z => z.Zone, ct);
        return Enum.GetValues<WarehouseZone>()
            .Select(zone => saved.TryGetValue(zone, out var z) ? new ZoneCheckDto(zone, z.Status, z.Notes) : new ZoneCheckDto(zone, null, null))
            .ToList();
    }
}

public sealed record SaveZoneChecksCommand(DateOnly Date, IReadOnlyList<ZoneCheckDto> Zones) : ICommand<Success>;

public sealed class SaveZoneChecksHandler(IAppDbContext db) : ICommandHandler<SaveZoneChecksCommand, Success>
{
    public async ValueTask<Success> Handle(SaveZoneChecksCommand command, CancellationToken ct)
    {
        var saved = await db.ZoneChecks.Where(z => z.BusinessDate == command.Date).ToDictionaryAsync(z => z.Zone, ct);
        foreach (var input in command.Zones.Where(z => z.Status is not null))
        {
            if (!saved.TryGetValue(input.Zone, out var check))
            {
                check = new ZoneCheck(command.Date, input.Zone);
                db.ZoneChecks.Add(check);
            }

            check.Update(input.Status!.Value, input.Notes?.Trim() is { Length: > 500 } n ? n[..500] : input.Notes?.Trim());
        }

        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
