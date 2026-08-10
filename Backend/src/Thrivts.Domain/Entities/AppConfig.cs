using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>Generic key-value app configuration blob (the live schema's app_config table).
/// Id is an arbitrary text key (e.g. "homepage"), not a uuid.</summary>
public class AppConfig : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string DataJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private AppConfig()
    {
        // EF Core
    }

    public AppConfig(string id, string dataJson = "{}")
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
