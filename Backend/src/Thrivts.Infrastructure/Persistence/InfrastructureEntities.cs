namespace Thrivts.Infrastructure.Persistence;

/// <summary>One row per serial series (lot:VEWL, sku:CDP, ...). Incremented atomically with an
/// UPSERT ... RETURNING, so numbers are never minted twice.</summary>
public class SerialCounter
{
    public string Name { get; set; } = default!;
    public long Value { get; set; }
}

/// <summary>
/// Remembers the response to a write request carrying an Idempotency-Key header. Warehouse devices
/// on weak wifi retry requests; replaying a key returns the original response instead of logging
/// the same arrival or payment twice.
/// </summary>
public class IdempotencyRecord
{
    public string Key { get; set; } = default!;
    public Guid UserId { get; set; }
    public string RequestHash { get; set; } = default!;
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public byte[]? ResponseBody { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
