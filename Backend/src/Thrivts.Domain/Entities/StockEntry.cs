using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>A warehouse stock-in record (the live schema's stock_entries table). Generic
/// id+jsonb shape — Id is an arbitrary text key, not a uuid; the entry's actual fields live in DataJson.</summary>
public class StockEntry : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string DataJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private StockEntry()
    {
        // EF Core
    }

    public StockEntry(string id, string dataJson = "{}")
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
