using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>An IP address blocked from the platform (the live schema's blocked_ips table).</summary>
public class BlockedIp : IAggregateRoot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string IpAddress { get; private set; } = default!;
    public string? Reason { get; private set; }
    public Guid? BlockedBy { get; private set; }
    public DateTimeOffset BlockedAt { get; private set; } = DateTimeOffset.UtcNow;

    private BlockedIp()
    {
        // EF Core
    }

    public BlockedIp(string ipAddress, string? reason = null, Guid? blockedBy = null)
    {
        IpAddress = ipAddress;
        Reason = reason;
        BlockedBy = blockedBy;
    }
}
