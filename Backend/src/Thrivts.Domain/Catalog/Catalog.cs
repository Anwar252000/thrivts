using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Catalog;

public class Supplier : AuditableEntity
{
    public const int MaxNameLength = 40;

    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? City { get; private set; }
    public string? Since { get; private set; }
    public string? Notes { get; private set; }
    public bool IsArchived { get; private set; }

    private Supplier()
    {
    }

    public Supplier(string name)
    {
        Rename(name);
    }

    public void Rename(string name)
    {
        var clean = Text.Clean(name);
        if (clean.Length == 0)
            throw new DomainException("Supplier name is required.");
        if (clean.Length > MaxNameLength)
            throw new DomainException($"Supplier name is too long (max {MaxNameLength} letters).");

        Name = clean;
        NormalizedName = Text.Normalize(clean);
    }

    public void UpdateDetails(string? phone, string? city, string? since, string? notes)
    {
        Phone = phone;
        City = city;
        Since = since;
        Notes = notes;
    }

    public void Archive() => IsArchived = true;

    public void Restore() => IsArchived = false;
}

/// <summary>Grading criteria per item (brands to look for, acceptable grades, reject rules).
/// Public information — shown to staff at packing time.</summary>
public class ItemCriteria
{
    public string? Brands { get; set; }
    public string? Grades { get; set; }
    public string? RejectIf { get; set; }
    public string? Notes { get; set; }
    public string? Style { get; set; }
}

/// <summary>The catalogue entry. Deliberately has NO price columns — prices live in
/// <see cref="ItemPrice"/>, a separate owner-only table, so no staff query can ever select one.</summary>
public class Item : AuditableEntity
{
    public const int MaxNameLength = 80;

    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public string? Category { get; private set; }
    public string? SkuCategory { get; private set; }
    public string? GradeNote { get; private set; }
    public decimal? WeightKg { get; private set; }
    public ItemCriteria Criteria { get; private set; } = new();
    public string? RejectRule { get; private set; }
    public bool IsArchived { get; private set; }

    private Item()
    {
    }

    public Item(string name, string? category)
    {
        Rename(name);
        Category = category;
    }

    public void Rename(string name)
    {
        var clean = Text.Clean(name);
        if (clean.Length == 0)
            throw new DomainException("Item name is required.");
        if (clean.Length > MaxNameLength)
            throw new DomainException($"Item name is too long (max {MaxNameLength} letters).");

        Name = clean;
        NormalizedName = Text.Normalize(clean);
    }

    public void UpdateDetails(string? category, string? skuCategory, string? gradeNote, decimal? weightKg, string? rejectRule)
    {
        if (weightKg is < 0)
            throw new DomainException("Weight cannot be negative.");

        Category = category;
        SkuCategory = skuCategory;
        GradeNote = gradeNote;
        WeightKg = weightKg;
        RejectRule = rejectRule;
    }

    public void UpdateCriteria(ItemCriteria criteria) => Criteria = criteria;

    public void Archive() => IsArchived = true;

    public void Restore() => IsArchived = false;
}

/// <summary>Owner-only. One buy price + one sell price per item (the agreed pricing model);
/// SellA/SellC are optional reference values kept from the older A/C model.</summary>
public class ItemPrice : AuditableEntity
{
    public Guid ItemId { get; private set; }
    public decimal? BuyPrice { get; private set; }
    public decimal? SellPrice { get; private set; }
    public decimal? SellA { get; private set; }
    public decimal? SellC { get; private set; }

    private ItemPrice()
    {
    }

    public ItemPrice(Guid itemId)
    {
        ItemId = itemId;
    }

    public void Set(decimal? buy, decimal? sell, decimal? sellA, decimal? sellC)
    {
        if (buy is < 0 || sell is < 0 || sellA is < 0 || sellC is < 0)
            throw new DomainException("Prices cannot be negative.");

        BuyPrice = buy;
        SellPrice = sell;
        SellA = sellA;
        SellC = sellC;
    }

    /// <summary>C grade sells at half the A price unless a C price is set explicitly.</summary>
    public decimal? PriceFor(bool isGradeC) => isGradeC
        ? SellC ?? (SellPrice is { } sell ? Math.Round(sell / 2, 2, MidpointRounding.AwayFromZero) : null)
        : SellPrice;
}

public class Labourer : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Labourer()
    {
    }

    public Labourer(string name)
    {
        var clean = Text.Clean(name);
        if (clean.Length is 0 or > 40)
            throw new DomainException("Worker name must be 1–40 letters.");

        Name = clean;
        NormalizedName = Text.Normalize(clean);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

/// <summary>Sack size per SKU category (Fleeces 35, T-Shirts 70, ...). Changing a size only
/// affects sacks packed afterwards; existing sacks keep the size they were packed with.</summary>
public class SkuCategory : AuditableEntity
{
    public const int DefaultSackSize = 50;

    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public int SackSize { get; private set; }

    private SkuCategory()
    {
    }

    public SkuCategory(string name, int sackSize)
    {
        var clean = Text.Clean(name);
        if (clean.Length is 0 or > 40)
            throw new DomainException("Category name must be 1–40 letters.");

        Name = clean;
        NormalizedName = Text.Normalize(clean);
        SetSackSize(sackSize);
    }

    public void SetSackSize(int size)
    {
        if (size is < 1 or > 1000)
            throw new DomainException("Sack size must be between 1 and 1000 pieces.");

        SackSize = size;
    }
}

public enum ItemRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>Staff propose a new item; the owner app (finance) prices, weighs and approves it,
/// which creates the catalogue item.</summary>
public class ItemRequest : AuditableEntity
{
    public string ProposedName { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public string? Note { get; private set; }
    public Guid RequestedById { get; private set; }
    public string RequestedByName { get; private set; } = default!;
    public ItemRequestStatus Status { get; private set; } = ItemRequestStatus.Pending;
    public Guid? DecidedById { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionNote { get; private set; }
    public Guid? ApprovedItemId { get; private set; }

    private ItemRequest()
    {
    }

    public ItemRequest(string proposedName, string? note, Guid requestedById, string requestedByName)
    {
        var clean = Text.Clean(proposedName);
        if (clean.Length is 0 or > Item.MaxNameLength)
            throw new DomainException($"Item name must be 1–{Item.MaxNameLength} letters.");

        ProposedName = clean;
        NormalizedName = Text.Normalize(clean);
        Note = note;
        RequestedById = requestedById;
        RequestedByName = requestedByName;
    }

    public void Approve(Guid itemId, Guid decidedBy, DateTimeOffset at, string? note)
    {
        EnsurePending();
        Status = ItemRequestStatus.Approved;
        ApprovedItemId = itemId;
        DecidedById = decidedBy;
        DecidedAt = at;
        DecisionNote = note;
    }

    public void Reject(Guid decidedBy, DateTimeOffset at, string? note)
    {
        EnsurePending();
        Status = ItemRequestStatus.Rejected;
        DecidedById = decidedBy;
        DecidedAt = at;
        DecisionNote = note;
    }

    private void EnsurePending()
    {
        if (Status != ItemRequestStatus.Pending)
            throw new DomainException("This request has already been decided.");
    }
}
