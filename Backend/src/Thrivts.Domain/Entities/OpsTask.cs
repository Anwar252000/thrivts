using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An internal admin/ops team task-board item (the live schema's ops_tasks table). Column names
/// are terse in the live schema (w, cat, own, p) — kept as-is rather than guessing full intent;
/// likely week/category/owner/priority given the shape, but unverified.
/// </summary>
public class OpsTask : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public int? Week { get; private set; }
    public string? Category { get; private set; }
    public string? Owner { get; private set; }
    public int? Priority { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Note { get; private set; }
    public DateOnly? Due { get; private set; }
    public string Status { get; private set; } = "todo";
    public long? Ts { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private OpsTask()
    {
        // EF Core
    }

    public OpsTask(string id, string title, string? category = null, string? owner = null,
        int? priority = null, int? week = null)
    {
        Id = id;
        Title = title;
        Category = category;
        Owner = owner;
        Priority = priority;
        Week = week;
    }

    public void SetStatus(string status, DateTimeOffset occurredAt)
    {
        Status = status;
        UpdatedAt = occurredAt;
    }

    public void SetDue(DateOnly? due) => Due = due;
}
