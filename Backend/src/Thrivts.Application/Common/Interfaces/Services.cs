using Thrivts.Domain.Identity;

namespace Thrivts.Application.Common.Interfaces;

/// <summary>The business runs on Pakistan time; "today" for the warehouse is always the Karachi
/// date, never the server's UTC date (the legacy early-morning wrong-date bug).</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
    DateOnly ToBusinessDate(DateTimeOffset instant);
}

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Name { get; }
    UserRole? Role { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }

    Guid RequireUserId();
}

public interface IPinHasher
{
    string Hash(string pin);
    bool Verify(string hash, string pin);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record IssuedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(AppUser user);
    IssuedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string token);
}

/// <summary>Gap-free, never-reused serial numbers (VEWL07, GATE-012, VEWC03, CDP14). Drawn from a
/// database counter atomically, so two devices can never mint the same number — the legacy app
/// minted from local data and collided.</summary>
public interface ISerialGenerator
{
    Task<long> NextAsync(string counter, CancellationToken cancellationToken);

    /// <summary>The number the next call to NextAsync would return — for previews only.</summary>
    Task<long> PeekAsync(string counter, CancellationToken cancellationToken);
}

public static class SerialCounters
{
    public const string Lot = "lot:VEWL";
    public const string GateExit = "gate:GATE";
    public const string ContainerDispatch = "dispatch:VEWC";
    public const string SmallDispatch = "dispatch:VEWO";
    public const string VendorInvoice = "invoice:INV";

    public static string Sku(string prefix) => $"sku:{prefix}";
}

public enum RealtimeAudience
{
    Staff,
    Owner,
    All,
}

/// <summary>Pushes "something changed" signals to connected apps so screens refresh themselves —
/// replaces the legacy manual "Sync now" buttons and 15-second polling.</summary>
public interface IRealtimeNotifier
{
    Task PublishAsync(string topic, RealtimeAudience audience, object? payload, CancellationToken cancellationToken);
}

public static class RealtimeTopics
{
    public const string Lots = "lots";
    public const string Stock = "stock";
    public const string Sku = "sku";
    public const string Orders = "orders";
    public const string Operations = "operations";
    public const string Escalations = "escalations";
    public const string Catalog = "catalog";
    public const string Finance = "finance";
    public const string Wash = "wash";
    public const string ClientMessages = "client-messages";
}

public interface ICacheService
{
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, string[] tags, CancellationToken cancellationToken);
    ValueTask InvalidateAsync(string tag, CancellationToken cancellationToken);
}

public static class CacheTags
{
    public const string Catalog = "catalog";
    public const string Suppliers = "suppliers";
    public const string SkuCategories = "sku-categories";
    public const string Labourers = "labourers";
}
