using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>A "wash sent" warehouse record (the live schema's wash_sent table — likely garments
/// sent out for a wash/cleaning process). Same generic id+jsonb shape as StockEntry.</summary>
public class WashSentEntry : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string DataJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private WashSentEntry()
    {
        // EF Core
    }

    public WashSentEntry(string id, string dataJson = "{}")
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
