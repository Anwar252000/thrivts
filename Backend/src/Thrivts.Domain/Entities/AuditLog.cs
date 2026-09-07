using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>An immutable record of a mutating admin/buyer/seller action (written here automatically
/// by AuditLoggingBehavior).</summary>
public class AuditLog : BaseEntity, IAggregateRoot
{
    public Guid? ActorId { get; private set; }
    public UserRole? ActorRole { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public Guid? EntityId { get; private set; }
    public string? DetailsJson { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private AuditLog()
    {
        // EF Core
    }

    public AuditLog(string action, string entityType, Guid? actorId = null, UserRole? actorRole = null,
        Guid? entityId = null, string? detailsJson = null, string? ipAddress = null, string? userAgent = null)
    {
        Action = action;
        EntityType = entityType;
        ActorId = actorId;
        ActorRole = actorRole;
        EntityId = entityId;
        DetailsJson = detailsJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
