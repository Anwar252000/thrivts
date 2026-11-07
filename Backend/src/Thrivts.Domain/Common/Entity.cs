namespace Thrivts.Domain.Common;

/// <summary>Every row's identity is a UUIDv7 — time-ordered, so inserts append to the end of the
/// primary-key index instead of scattering across it (matters once tables reach lakhs of rows).</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}

/// <summary>Created/updated stamps are set by the persistence layer from the authenticated user,
/// never by request payloads.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    Guid? CreatedById { get; }
    DateTimeOffset? UpdatedAt { get; }
    Guid? UpdatedById { get; }

    void StampCreated(DateTimeOffset at, Guid? by);
    void StampUpdated(DateTimeOffset at, Guid? by);
}

public abstract class AuditableEntity : Entity, IAuditable
{
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedById { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedById { get; private set; }

    public void StampCreated(DateTimeOffset at, Guid? by)
    {
        CreatedAt = at;
        CreatedById = by;
    }

    public void StampUpdated(DateTimeOffset at, Guid? by)
    {
        UpdatedAt = at;
        UpdatedById = by;
    }
}

/// <summary>Soft delete: a delete becomes an update that sets these fields, and a global query
/// filter hides the row. Nothing that touched money or stock is ever physically removed.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    Guid? DeletedById { get; }

    void MarkDeleted(DateTimeOffset at, Guid? by);
}

/// <summary>Optimistic concurrency via Postgres xmin: two devices editing the same row can no
/// longer silently overwrite each other — the second save gets a 409 instead.</summary>
public interface IVersioned
{
    uint Version { get; }
}
