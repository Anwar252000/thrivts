using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// Online/offline presence for an internal admin/ops team member (the live schema's ops_presence
/// table). "Who" is the primary key — a free-text identifier for the team member, not a uuid.
/// </summary>
public class OpsPresence : IAggregateRoot
{
    public string Who { get; private set; } = default!;
    public string? Name { get; private set; }
    public bool Online { get; private set; }
    public long? Ts { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private OpsPresence()
    {
        // EF Core
    }

    public OpsPresence(string who, string? name = null)
    {
        Who = who;
        Name = name;
    }

    public void SetOnline(bool online, long? ts, DateTimeOffset occurredAt)
    {
        Online = online;
        Ts = ts;
        UpdatedAt = occurredAt;
    }
}
