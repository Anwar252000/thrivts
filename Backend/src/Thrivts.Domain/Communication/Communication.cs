using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Communication;

public enum EscalationSource
{
    Message,
    Blocker,
}

/// <summary>Any staff member → the owner, two-way: the owner replies and resolves; the sender sees
/// the reply flagged as unread until they open it.</summary>
public class Escalation : AuditableEntity
{
    public Guid FromUserId { get; private set; }
    public string FromName { get; private set; } = default!;
    public EscalationSource Source { get; private set; }
    public string Message { get; private set; } = default!;
    public string? Department { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string? Reply { get; private set; }
    public DateTimeOffset? RepliedAt { get; private set; }
    public Guid? RepliedById { get; private set; }
    public DateTimeOffset? ReplyReadAt { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private Escalation()
    {
    }

    public Escalation(Guid fromUserId, string fromName, EscalationSource source, string message, string? department, DateOnly businessDate)
    {
        var clean = message.Trim();
        if (clean.Length is 0 or > 2000)
            throw new DomainException("Message must be 1–2000 characters.");

        FromUserId = fromUserId;
        FromName = fromName;
        Source = source;
        Message = clean;
        Department = department;
        BusinessDate = businessDate;
    }

    public bool HasUnreadReply => Reply is not null && ReplyReadAt is null;

    public void ReplyWith(string reply, Guid by, DateTimeOffset at)
    {
        var clean = reply.Trim();
        if (clean.Length is 0 or > 2000)
            throw new DomainException("Reply must be 1–2000 characters.");

        Reply = clean;
        RepliedAt = at;
        RepliedById = by;
        ReplyReadAt = null;
    }

    public void MarkReplyRead(DateTimeOffset at) => ReplyReadAt ??= at;

    public void Resolve(DateTimeOffset at)
    {
        IsResolved = true;
        ResolvedAt = at;
    }

    public void Reopen()
    {
        IsResolved = false;
        ResolvedAt = null;
    }
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
}

public class Approval : AuditableEntity
{
    public Guid? FromUserId { get; private set; }
    public string FromName { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? ValueText { get; private set; }
    public ApprovalStatus Status { get; private set; } = ApprovalStatus.Pending;
    public DateTimeOffset? DecidedAt { get; private set; }
    public Guid? DecidedById { get; private set; }

    private Approval()
    {
    }

    public Approval(Guid? fromUserId, string fromName, string description, string? valueText)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500)
            throw new DomainException("Description must be 1–500 characters.");

        FromUserId = fromUserId;
        FromName = fromName;
        Description = description.Trim();
        ValueText = valueText;
    }

    public void Decide(bool approve, Guid by, DateTimeOffset at)
    {
        if (Status != ApprovalStatus.Pending)
            throw new DomainException("This approval has already been decided.");

        Status = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        DecidedById = by;
        DecidedAt = at;
    }
}

public enum GoalPeriod
{
    Monthly,
    SixMonth,
    Yearly,
}

public class Goal : AuditableEntity
{
    public GoalPeriod Period { get; private set; }
    public string Label { get; private set; } = default!;
    public string? Target { get; private set; }
    public int Progress { get; private set; }

    private Goal()
    {
    }

    public Goal(GoalPeriod period, string label, string? target, int progress)
    {
        Update(period, label, target, progress);
    }

    public void Update(GoalPeriod period, string label, string? target, int progress)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Length > 200)
            throw new DomainException("Goal label must be 1–200 characters.");

        Period = period;
        Label = label.Trim();
        Target = target;
        Progress = Math.Clamp(progress, 0, 100);
    }
}

public enum ActivityArea
{
    Gate,
    Lots,
    Inventory,
    Sku,
    Dispatch,
    Orders,
    Operations,
    Wash,
}

/// <summary>Human-readable activity feed (the Zone 4 activity log, dashboard timelines). Append-only;
/// the full field-level change history lives in the audit log.</summary>
public class ActivityEvent : Entity
{
    public ActivityArea Area { get; private set; }
    public string Type { get; private set; } = default!;
    public string Text { get; private set; } = default!;
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? ReferenceId { get; private set; }

    private ActivityEvent()
    {
    }

    public ActivityEvent(ActivityArea area, string type, string text, Guid? userId, string? userName, DateTimeOffset occurredAt, Guid? referenceId = null)
    {
        Area = area;
        Type = type;
        Text = text.Length > 1000 ? text[..1000] : text;
        UserId = userId;
        UserName = userName;
        OccurredAt = occurredAt;
        ReferenceId = referenceId;
    }
}
