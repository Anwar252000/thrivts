using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Operations;

public enum DailySection
{
    Requirements,
    TeamCheck,
    Expenditure,
    Blockers,
    Warehouse,
}

/// <summary>One per user, section and business date — "submitted" or "no progress today". Drives
/// the GM's 30-day compliance dashboard.</summary>
public class DailySubmission : Entity
{
    public Guid UserId { get; private set; }
    public DailySection Section { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public bool IsNoProgress { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    private DailySubmission()
    {
    }

    public DailySubmission(Guid userId, DailySection section, DateOnly businessDate, bool isNoProgress, DateTimeOffset submittedAt)
    {
        UserId = userId;
        Section = section;
        BusinessDate = businessDate;
        IsNoProgress = isNoProgress;
        SubmittedAt = submittedAt;
    }

    /// <summary>A real submission always replaces a "no progress" mark; a "no progress" mark never
    /// overwrites a real submission (the legacy app let it, wiping that day's record).</summary>
    public void Resubmit(bool isNoProgress, DateTimeOffset at)
    {
        if (isNoProgress && !IsNoProgress)
            return;

        IsNoProgress = isNoProgress;
        SubmittedAt = at;
    }
}

public enum ExpenseCategory
{
    PettyCash,
    Salary,
    Utility,
    Laundry,
    Transport,
    Other,
}

public class Expense : AuditableEntity, ISoftDeletable
{
    public DateOnly BusinessDate { get; private set; }
    public string Description { get; private set; } = default!;
    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public bool HasReceipt { get; private set; }
    public string? ReceiptLink { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    private Expense()
    {
    }

    public Expense(DateOnly businessDate, string description, ExpenseCategory category, decimal amount, bool hasReceipt, string? receiptLink)
    {
        BusinessDate = businessDate;
        Update(description, category, amount, hasReceipt, receiptLink);
    }

    public void Update(string description, ExpenseCategory category, decimal amount, bool hasReceipt, string? receiptLink)
    {
        var clean = Text.Clean(description);
        if (clean.Length is 0 or > 200)
            throw new DomainException("Describe the expense (max 200 characters).");
        if (amount is < 0 or > 100_000_000)
            throw new DomainException("Enter a valid amount.");

        Description = clean;
        Category = category;
        Amount = amount;
        HasReceipt = hasReceipt;
        ReceiptLink = string.IsNullOrWhiteSpace(receiptLink) ? null : receiptLink.Trim();
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}

public enum Department
{
    General,
    Labour,
    Inventory,
    Supply,
    Marketing,
    Support,
}

public class Blocker : AuditableEntity, ISoftDeletable
{
    public DateOnly BusinessDate { get; private set; }
    public string Description { get; private set; } = default!;
    public Department Department { get; private set; }
    public string? Responsible { get; private set; }
    public bool NeedsOwner { get; private set; }
    public Guid? EscalationId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    private Blocker()
    {
    }

    public Blocker(DateOnly businessDate, string description, Department department, string? responsible, bool needsOwner)
    {
        BusinessDate = businessDate;
        Update(description, department, responsible, needsOwner);
    }

    public void Update(string description, Department department, string? responsible, bool needsOwner)
    {
        var clean = Text.Clean(description);
        if (clean.Length is 0 or > 500)
            throw new DomainException("Describe what happened (max 500 characters).");

        Description = clean;
        Department = department;
        Responsible = string.IsNullOrWhiteSpace(responsible) ? null : responsible.Trim();
        NeedsOwner = needsOwner;
    }

    public void LinkEscalation(Guid escalationId) => EscalationId = escalationId;

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}

public enum DayRating
{
    Great,
    Good,
    Okay,
    Tough,
    Bad,
}

public enum OnTrackStatus
{
    AllOnTrack,
    MostOnTrack,
    SomeBehind,
    Behind,
}

/// <summary>End-of-day summary for one business date. Stored on its own so it survives even on
/// days with zero blockers (the legacy table lost it then).</summary>
public class DaySummary : AuditableEntity
{
    public DateOnly BusinessDate { get; private set; }
    public DayRating Rating { get; private set; } = DayRating.Okay;
    public OnTrackStatus? OnTrack { get; private set; }
    public string? FocusTomorrow { get; private set; }
    public string? OwnerActions { get; private set; }
    public string? TeamReport { get; private set; }

    private DaySummary()
    {
    }

    public DaySummary(DateOnly businessDate)
    {
        BusinessDate = businessDate;
    }

    public void Update(DayRating rating, OnTrackStatus? onTrack, string? focusTomorrow, string? ownerActions)
    {
        Rating = rating;
        OnTrack = onTrack;
        FocusTomorrow = focusTomorrow;
        OwnerActions = ownerActions;
    }

    public void SetTeamReport(string? report) => TeamReport = report;
}

public enum AttendanceMark
{
    Yes,
    No,
    HalfDay,
    Leave,
}

public enum Punctuality
{
    OnTime,
    Late,
    NotApplicable,
}

public enum EntryStatus
{
    Yes,
    No,
    Partial,
    NotApplicable,
}

public class TeamCheck : AuditableEntity
{
    public DateOnly BusinessDate { get; private set; }
    public string PersonName { get; private set; } = default!;
    public string RoleLabel { get; private set; } = default!;
    public AttendanceMark Attended { get; private set; }
    public Punctuality Punctuality { get; private set; }
    public EntryStatus EntrySubmitted { get; private set; }
    public bool HasIssues { get; private set; }
    public string? Notes { get; private set; }

    private TeamCheck()
    {
    }

    public TeamCheck(DateOnly businessDate, string personName, string roleLabel)
    {
        BusinessDate = businessDate;
        PersonName = Text.Clean(personName);
        RoleLabel = Text.Clean(roleLabel);
    }

    public void Update(AttendanceMark attended, Punctuality punctuality, EntryStatus entrySubmitted, bool hasIssues, string? notes)
    {
        Attended = attended;
        Punctuality = punctuality;
        EntrySubmitted = entrySubmitted;
        HasIssues = hasIssues;
        Notes = notes;
    }
}

public enum WarehouseZone
{
    StockInGrading,
    Washing,
    GradedInventory,
    SkuSacks,
    FinishedStock,
    Dispatch,
}

public enum ZoneStatus
{
    Organised,
    NeedsWork,
    Disorganised,
}

public class ZoneCheck : AuditableEntity
{
    public DateOnly BusinessDate { get; private set; }
    public WarehouseZone Zone { get; private set; }
    public ZoneStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private ZoneCheck()
    {
    }

    public ZoneCheck(DateOnly businessDate, WarehouseZone zone)
    {
        BusinessDate = businessDate;
        Zone = zone;
    }

    public void Update(ZoneStatus status, string? notes)
    {
        Status = status;
        Notes = notes;
    }
}

/// <summary>Worker attendance for one day. Overtime is computed here, server-side, so every
/// report agrees: any time after 6 PM, or the whole shift on a Sunday. Arriving early never counts.</summary>
public class AttendanceRecord : AuditableEntity
{
    public static readonly TimeOnly DefaultIn = new(9, 0);
    public static readonly TimeOnly DefaultOut = new(18, 0);
    private static readonly TimeOnly OvertimeStarts = new(18, 0);

    public DateOnly BusinessDate { get; private set; }
    public Guid LabourerId { get; private set; }
    public string LabourerName { get; private set; } = default!;
    public TimeOnly TimeIn { get; private set; }
    public TimeOnly TimeOut { get; private set; }
    public decimal OvertimeHours { get; private set; }
    public bool IsSunday { get; private set; }

    private AttendanceRecord()
    {
    }

    public AttendanceRecord(DateOnly businessDate, Guid labourerId, string labourerName)
    {
        BusinessDate = businessDate;
        LabourerId = labourerId;
        LabourerName = labourerName;
        IsSunday = businessDate.DayOfWeek == DayOfWeek.Sunday;
        SetTimes(DefaultIn, DefaultOut);
    }

    public void SetTimes(TimeOnly timeIn, TimeOnly timeOut)
    {
        TimeIn = timeIn;
        TimeOut = timeOut;
        OvertimeHours = ComputeOvertime(BusinessDate, timeIn, timeOut);
    }

    /// <summary>TimeOnly subtraction wraps around midnight, so out-before-in (a shift past
    /// midnight) is guarded first and counts as 0, matching the legacy rule.</summary>
    public static decimal ComputeOvertime(DateOnly date, TimeOnly timeIn, TimeOnly timeOut)
    {
        if (timeOut <= timeIn)
            return 0;

        var start = date.DayOfWeek == DayOfWeek.Sunday || timeIn > OvertimeStarts ? timeIn : OvertimeStarts;
        if (timeOut <= start)
            return 0;

        return Math.Round((decimal)(timeOut - start).TotalHours, 2, MidpointRounding.AwayFromZero);
    }
}
