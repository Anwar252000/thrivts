using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An immutable record of an admin action (the live schema's audit_log table — every admin
/// action is written here automatically per the migration plan §6.4). README_HANDOVER.md §4
/// flags a display bug ("Kyc" should read "KYC") that belongs in the presentation layer, not here.
/// </summary>
public class AuditLog : BaseEntity, IAggregateRoot
{
    public Guid ActorProfileId { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string? Details { get; private set; }

    private AuditLog()
    {
        // EF Core
    }

    public AuditLog(Guid actorProfileId, string action, string entityType, Guid entityId, string? details = null)
    {
        ActorProfileId = actorProfileId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details;
    }
}
