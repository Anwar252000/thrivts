namespace Thrivts.Domain.Common;

public enum FilePurpose
{
    Signature,
}

/// <summary>
/// Small binary attachments (signature PNGs, a few KB each) kept in their own table and fetched
/// only on demand by id. This is the fix for the legacy 271 KB gate-log blob, where every
/// signature rode along on every sync.
/// </summary>
public class StoredFile : Entity
{
    public FilePurpose Purpose { get; private set; }
    public string ContentType { get; private set; } = default!;
    public int SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = default!;
    public byte[] Content { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedById { get; private set; }

    private StoredFile()
    {
    }

    public StoredFile(FilePurpose purpose, string contentType, byte[] content, string sha256, DateTimeOffset createdAt, Guid? createdById)
    {
        Purpose = purpose;
        ContentType = contentType;
        Content = content;
        SizeBytes = content.Length;
        Sha256 = sha256;
        CreatedAt = createdAt;
        CreatedById = createdById;
    }
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
}

/// <summary>Who changed what, when — written automatically for every change to an auditable row.
/// Append-only.</summary>
public class AuditLog : Entity
{
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string? Changes { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(DateTimeOffset occurredAt, Guid? userId, string? userName, AuditAction action, string entityType, Guid entityId, string? changes, string? correlationId, string? ipAddress)
    {
        OccurredAt = occurredAt;
        UserId = userId;
        UserName = userName;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Changes = changes;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
    }
}
