using System.Text;
using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;
using Thrivts.Domain.Inventory;

namespace Thrivts.Domain.Sku;

public enum SackStatus
{
    Live,
    Shipped,
    Emptied,
}

public enum SackDestination
{
    None,
    Order,
    Warehouse,
}

/// <summary>
/// A packed sack (SKU code like CDP07). Its pieces stay in OnHand but are counted in InSacks on the
/// stock balances its components came from — packing never reduces inventory, only shipping does.
/// </summary>
public class Sack : AuditableEntity, IVersioned
{
    private readonly List<SackComponent> _components = [];

    public string Code { get; private set; } = default!;
    public string Prefix { get; private set; } = default!;
    public string ItemName { get; private set; } = default!;
    public string NormalizedItemName { get; private set; } = default!;
    public string SkuCategory { get; private set; } = default!;
    public int Size { get; private set; }
    public bool IsCustom { get; private set; }
    public int QtyA { get; private set; }
    public int QtyC { get; private set; }
    public SackStatus Status { get; private set; } = SackStatus.Live;
    public SackDestination Destination { get; private set; } = SackDestination.None;
    public Guid? ClientOrderId { get; private set; }
    public Guid? OrderLineId { get; private set; }
    public string? OrderName { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset PackedAt { get; private set; }
    public bool CriteriaConfirmed { get; private set; }
    public string? CriteriaNote { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyList<SackComponent> Components => _components;

    private Sack()
    {
    }

    public Sack(string code, string prefix, string itemName, string skuCategory, int size, bool isCustom, bool criteriaConfirmed, string? criteriaNote, DateTimeOffset packedAt)
    {
        if (!criteriaConfirmed)
            throw new DomainException("Confirm the sack meets the item criteria before packing.");

        Code = code;
        Prefix = prefix;
        ItemName = itemName;
        NormalizedItemName = Text.Normalize(itemName);
        SkuCategory = skuCategory;
        Size = size;
        IsCustom = isCustom;
        CriteriaConfirmed = true;
        CriteriaNote = string.IsNullOrWhiteSpace(criteriaNote) ? null : criteriaNote.Trim();
        PackedAt = packedAt;
    }

    public int Pieces => QtyA + QtyC;

    public bool IsOpened => Pieces < Size;

    public bool IsAssigned => Destination != SackDestination.None;

    public void AddComponent(StockBalance source, int quantity)
    {
        EnsureLive();
        if (quantity <= 0)
            throw new DomainException("Component quantity must be positive.");

        var existing = _components.FirstOrDefault(c => c.StockBalanceId == source.Id);
        if (existing is null)
            _components.Add(new SackComponent(Id, source, quantity));
        else
            existing.Add(quantity);

        if (source.Grade == Grade.A) QtyA += quantity; else QtyC += quantity;
    }

    /// <summary>Takes pieces of one grade back out, last-in first-out across components. Returns
    /// what was removed per stock balance so the caller can release InSacks.</summary>
    public IReadOnlyList<(Guid StockBalanceId, int Quantity)> TakeOut(Grade grade, int quantity)
    {
        EnsureLive();
        if (IsAssigned)
            throw new DomainException("Unassign the sack before taking pieces out.");
        var held = grade == Grade.A ? QtyA : QtyC;
        if (quantity <= 0 || quantity > held)
            throw new DomainException($"This sack holds only {held} {grade} pieces.");

        var released = new List<(Guid, int)>();
        var remaining = quantity;
        foreach (var component in _components.Where(c => c.Grade == grade).Reverse().ToList())
        {
            if (remaining == 0)
                break;

            var take = Math.Min(component.Quantity, remaining);
            component.Remove(take);
            released.Add((component.StockBalanceId, take));
            remaining -= take;
            if (component.Quantity == 0)
                _components.Remove(component);
        }

        if (grade == Grade.A) QtyA -= quantity; else QtyC -= quantity;
        if (Pieces == 0)
            Status = SackStatus.Emptied;

        return released;
    }

    public int SpaceLeft => Math.Max(0, Size - Pieces);

    public void EnsureCanTopUp(int adding)
    {
        EnsureLive();
        if (IsAssigned)
            throw new DomainException("Unassign the sack before topping it up.");
        if (adding > SpaceLeft)
            throw new DomainException($"Only {SpaceLeft} pieces fit in this sack.");
    }

    public void AssignToWarehouse(DateTimeOffset now)
    {
        EnsureLive();
        EnsureUnassigned();
        Destination = SackDestination.Warehouse;
        AssignedAt = now;
    }

    public void AssignToOrder(Guid clientOrderId, Guid orderLineId, string orderName, DateTimeOffset now)
    {
        EnsureLive();
        EnsureUnassigned();
        Destination = SackDestination.Order;
        ClientOrderId = clientOrderId;
        OrderLineId = orderLineId;
        OrderName = orderName;
        AssignedAt = now;
    }

    public void Unassign()
    {
        EnsureLive();
        Destination = SackDestination.None;
        ClientOrderId = null;
        OrderLineId = null;
        OrderName = null;
        AssignedAt = null;
    }

    public void MarkShipped(DateTimeOffset now)
    {
        EnsureLive();
        if (Destination != SackDestination.Order)
            throw new DomainException("Only a sack assigned to an order can be shipped.");

        Status = SackStatus.Shipped;
        ShippedAt = now;
    }

    private void EnsureLive()
    {
        if (Status != SackStatus.Live)
            throw new DomainException($"Sack {Code} is no longer in stock.");
    }

    private void EnsureUnassigned()
    {
        if (IsAssigned)
            throw new DomainException($"Sack {Code} is already assigned.");
    }
}

public class SackComponent : Entity
{
    public Guid SackId { get; private set; }
    public Guid StockBalanceId { get; private set; }
    public Guid LotId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public string ItemName { get; private set; } = default!;
    public Grade Grade { get; private set; }
    public int Quantity { get; private set; }

    private SackComponent()
    {
    }

    internal SackComponent(Guid sackId, StockBalance source, int quantity)
    {
        SackId = sackId;
        StockBalanceId = source.Id;
        LotId = source.LotId;
        LotSerial = source.LotSerial;
        ItemName = source.ItemName;
        Grade = source.Grade;
        Quantity = quantity;
    }

    internal void Add(int quantity) => Quantity += quantity;

    internal void Remove(int quantity) => Quantity -= quantity;
}

/// <summary>The sticky SKU prefix for an item (CDP for "Carhartt Detroit Pants"). Unique across
/// items; once assigned it never changes for that item.</summary>
public class SkuPrefix : AuditableEntity
{
    public const int MaxLength = 8;

    public string NormalizedItemName { get; private set; } = default!;
    public string ItemName { get; private set; } = default!;
    public string Prefix { get; private set; } = default!;

    private SkuPrefix()
    {
    }

    public SkuPrefix(string itemName, string prefix)
    {
        ItemName = itemName;
        NormalizedItemName = Text.Normalize(itemName);
        Prefix = prefix;
    }

    public void Change(string prefix) => Prefix = prefix;
}

public enum SkuDispatchKind
{
    Loose,
    Shipped,
}

/// <summary>Pieces leaving through Zone 4 — loose pieces straight from the pool, or whole sacks
/// shipped against an order. These reduce OnHand.</summary>
public class SkuDispatch : AuditableEntity
{
    private readonly List<SkuDispatchComponent> _components = [];

    public SkuDispatchKind Kind { get; private set; }
    public string ItemName { get; private set; } = default!;
    public Grade? Grade { get; private set; }
    public int Quantity { get; private set; }
    public string? Reference { get; private set; }
    public Guid? ClientOrderId { get; private set; }
    public string? OrderName { get; private set; }
    public string? SackCode { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public IReadOnlyList<SkuDispatchComponent> Components => _components;

    private SkuDispatch()
    {
    }

    public SkuDispatch(SkuDispatchKind kind, string itemName, Grade? grade, string? reference, Guid? clientOrderId, string? orderName, string? sackCode, DateTimeOffset occurredAt)
    {
        Kind = kind;
        ItemName = itemName;
        Grade = grade;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        ClientOrderId = clientOrderId;
        OrderName = orderName;
        SackCode = sackCode;
        OccurredAt = occurredAt;
    }

    public void AddComponent(Guid stockBalanceId, string lotSerial, Grade grade, int quantity)
    {
        _components.Add(new SkuDispatchComponent(Id, stockBalanceId, lotSerial, grade, quantity));
        Quantity += quantity;
    }
}

public class SkuDispatchComponent : Entity
{
    public Guid SkuDispatchId { get; private set; }
    public Guid StockBalanceId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public Grade Grade { get; private set; }
    public int Quantity { get; private set; }

    private SkuDispatchComponent()
    {
    }

    internal SkuDispatchComponent(Guid dispatchId, Guid stockBalanceId, string lotSerial, Grade grade, int quantity)
    {
        SkuDispatchId = dispatchId;
        StockBalanceId = stockBalanceId;
        LotSerial = lotSerial;
        Grade = grade;
        Quantity = quantity;
    }
}

public static class SkuRules
{
    public const int MaxSacksPerBatch = 200;

    /// <summary>Blend presets. C is rounded half-up, per the owner's rule (90/10 of 35 → 4 C).</summary>
    public static (int A, int C) Blend(string preset, int size) => preset switch
    {
        "standard" => (size - RoundHalfUp(size * 0.1m), RoundHalfUp(size * 0.1m)),
        "half" => (size - RoundHalfUp(size * 0.5m), RoundHalfUp(size * 0.5m)),
        "allA" => (size, 0),
        _ => throw new DomainException("Unknown blend."),
    };

    public static string FormatCode(string prefix, long sequence) => $"{prefix}{sequence:00}";

    public static string SanitizePrefix(string prefix)
    {
        var sb = new StringBuilder();
        foreach (var ch in prefix.ToUpperInvariant())
        {
            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
                sb.Append(ch);
            if (sb.Length == SkuPrefix.MaxLength)
                break;
        }

        return sb.ToString();
    }

    /// <summary>Initials of the item's words (CDP); on a clash with another item's prefix, the
    /// last word is extended letter by letter, then a number is appended.</summary>
    public static string SuggestPrefix(string itemName, string category, ISet<string> takenPrefixes)
    {
        var words = itemName.Split([' ', '-', '/', '(', ')', '.', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant())
            .Where(w => w.Length > 0)
            .ToList();

        var initials = string.Concat(words.Select(w => w[0]));
        var basePrefix = initials.Length >= 2
            ? initials
            : SanitizePrefix(category).Length >= 3 ? SanitizePrefix(category)[..3] : "SKU";
        basePrefix = SanitizePrefix(basePrefix);

        if (!takenPrefixes.Contains(basePrefix))
            return basePrefix;

        if (words.Count > 0)
        {
            var head = string.Concat(words.Take(words.Count - 1).Select(w => w[0]));
            var last = words[^1];
            for (var k = 2; k <= Math.Min(last.Length, 6); k++)
            {
                var candidate = SanitizePrefix(head + last[..k]);
                if (!takenPrefixes.Contains(candidate))
                    return candidate;
            }
        }

        for (var n = 2; n < 1000; n++)
        {
            var candidate = SanitizePrefix(basePrefix[..Math.Min(basePrefix.Length, SkuPrefix.MaxLength - n.ToString().Length)] + n);
            if (!takenPrefixes.Contains(candidate))
                return candidate;
        }

        throw new DomainException("Could not find a free SKU prefix — set one manually.");
    }

    private static int RoundHalfUp(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
