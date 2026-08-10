using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>A warehouse stock-out record (the live schema's stockout_entries table). Same generic
/// id+jsonb shape as StockEntry — see its remarks.</summary>
public class StockoutEntry : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string DataJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private StockoutEntry()
    {
        // EF Core
    }

    public StockoutEntry(string id, string dataJson = "{}")
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
