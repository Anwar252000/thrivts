using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>A "wash received" warehouse record (the live schema's wash_received table — likely
/// garments received back from a wash/cleaning process). Same generic id+jsonb shape as StockEntry.</summary>
public class WashReceivedEntry : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string DataJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private WashReceivedEntry()
    {
        // EF Core
    }

    public WashReceivedEntry(string id, string dataJson = "{}")
    {
        Id = id;
        DataJson = dataJson;
    }

    public void SetData(string dataJson, DateTimeOffset occurredAt)
    {
        DataJson = dataJson;
        UpdatedAt = occurredAt;
    }
}
