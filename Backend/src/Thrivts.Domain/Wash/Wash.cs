using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;
using Thrivts.Domain.Inventory;

namespace Thrivts.Domain.Wash;

/// <summary>The washer's price categories (legacy WASH_CATS) and the keyword rules that guess an
/// item's category from its name. A per-item override always wins over the rules.</summary>
public static class WashCategories
{
    public const string Mix = "Mix";

    public static readonly IReadOnlyList<string> All =
    [
        "Fleeces", "Puffers", "Jackets", "Hoodies", "Sweatshirts", "Sweaters", "Knitwear", "Pants",
        "Jeans", "Shorts", "Shirts", "Polos", "T-Shirts", "Tracksuits", "Coats", Mix,
    ];

    // Order matters: the first keyword found wins ("puffer jacket" is a Puffer, not a Jacket).
    private static readonly (string Keyword, string Category)[] Rules =
    [
        ("fleece", "Fleeces"), ("puffer", "Puffers"), ("down jacket", "Puffers"), ("jacket", "Jackets"),
        ("hoodie", "Hoodies"), ("hooded", "Hoodies"), ("sweatshirt", "Sweatshirts"), ("crewneck", "Sweatshirts"),
        ("sweater", "Sweaters"), ("pullover", "Sweaters"), ("cardigan", "Knitwear"), ("knit", "Knitwear"),
        ("jean", "Jeans"), ("denim", "Jeans"), ("short", "Shorts"), ("jogger", "Pants"), ("cargo", "Pants"),
        ("trouser", "Pants"), ("chino", "Pants"), ("pant", "Pants"), ("polo", "Polos"), ("t-shirt", "T-Shirts"),
        ("tshirt", "T-Shirts"), ("t shirt", "T-Shirts"), ("tracksuit", "Tracksuits"), ("track", "Tracksuits"),
        ("coat", "Coats"), ("shirt", "Shirts"),
    ];

    public static bool IsValid(string category) => All.Contains(category);

    public static string Classify(string itemName)
    {
        var key = itemName.Trim().ToLowerInvariant();
        foreach (var (keyword, category) in Rules)
        {
            if (key.Contains(keyword, StringComparison.Ordinal))
                return category;
        }

        return Mix;
    }
}

/// <summary>A washing contractor (legacy: Irfan, Kashif) with a per-piece rate for each category.</summary>
public class WashVendor : AuditableEntity
{
    public const int MaxNameLength = 60;

    private readonly List<WashRate> _rates = [];

    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<WashRate> Rates => _rates;

    private WashVendor()
    {
    }

    public WashVendor(string name)
    {
        var clean = Text.Clean(name);
        if (clean.Length is 0 or > MaxNameLength)
            throw new DomainException($"Vendor name must be 1–{MaxNameLength} characters.");

        Name = clean;
        NormalizedName = Text.Normalize(clean);
    }

    public decimal RateFor(string category) => _rates.FirstOrDefault(r => r.Category == category)?.RatePerPiece ?? 0;

    public void SetRate(string category, decimal rate)
    {
        if (!WashCategories.IsValid(category))
            throw new DomainException($"Unknown wash category '{category}'.");
        if (rate is < 0 or > 100_000)
            throw new DomainException("Wash rate must be between 0 and 100,000 per piece.");

        var existing = _rates.FirstOrDefault(r => r.Category == category);
        if (existing is null)
            _rates.Add(new WashRate(Id, category, rate));
        else
            existing.Set(rate);
    }

    public void SetActive(bool active) => IsActive = active;
}

public class WashRate : Entity
{
    public Guid VendorId { get; private set; }
    public string Category { get; private set; } = default!;
    public decimal RatePerPiece { get; private set; }

    private WashRate()
    {
    }

    internal WashRate(Guid vendorId, string category, decimal rate)
    {
        VendorId = vendorId;
        Category = category;
        RatePerPiece = rate;
    }

    internal void Set(decimal rate) => RatePerPiece = rate;
}

/// <summary>"This item washes as a Hoodie" — remembered once, used for every future wash.</summary>
public class WashItemCategory : Entity
{
    public string NormalizedItemName { get; private set; } = default!;
    public string Category { get; private set; } = default!;

    private WashItemCategory()
    {
    }

    public WashItemCategory(string itemName, string category)
    {
        NormalizedItemName = Text.Normalize(itemName);
        Set(category);
    }

    public void Set(string category)
    {
        if (!WashCategories.IsValid(category))
            throw new DomainException($"Unknown wash category '{category}'.");

        Category = category;
    }
}

public enum WashStatus
{
    /// <summary>At the washer, nothing back yet.</summary>
    Sent,

    /// <summary>Some pieces back, some still at the washer.</summary>
    Partial,

    /// <summary>Every piece accounted for; the bill can be finalized.</summary>
    Received,

    /// <summary>Locked for payment.</summary>
    Finalized,
}

public enum WashPaymentStatus
{
    Unpaid,
    Partial,
    Paid,
}

/// <summary>
/// One trip to the washer. A wash keeps its source lot's identity (legacy rule: one lot per wash,
/// the wash keeps the VEWL number), so its number is the lot serial plus a sequence: VEWL07-W1.
/// The washer is paid only for the pieces that come back good (A or C).
/// </summary>
public class WashBatch : AuditableEntity, ISoftDeletable, IVersioned
{
    public const int MaxLines = 200;

    private readonly List<WashLine> _lines = [];

    public string Number { get; private set; } = default!;
    public Guid LotId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = default!;
    public DateOnly SentDate { get; private set; }
    public DateOnly? ReceivedDate { get; private set; }
    public WashStatus Status { get; private set; } = WashStatus.Sent;
    public WashPaymentStatus PaymentStatus { get; private set; } = WashPaymentStatus.Unpaid;
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedById { get; private set; }
    public string? Notes { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyList<WashLine> Lines => _lines;

    private WashBatch()
    {
    }

    public WashBatch(string number, Guid lotId, string lotSerial, WashVendor vendor, DateOnly sentDate, string? notes)
    {
        if (!vendor.IsActive)
            throw new DomainException($"{vendor.Name} is no longer an active wash vendor.");

        Number = number;
        LotId = lotId;
        LotSerial = lotSerial;
        VendorId = vendor.Id;
        VendorName = vendor.Name;
        SentDate = sentDate;
        Notes = notes;
    }

    public int SentPieces => _lines.Sum(l => l.Sent);

    public int StillOut => _lines.Sum(l => l.StillOut);

    /// <summary>What the washer is owed: good pieces back × the agreed rate.</summary>
    public decimal AmountDue => _lines.Sum(l => l.AmountDue);

    public WashLine AddLine(StockBalance source, string category, int pieces, decimal pricePerPiece)
    {
        if (source.LotId != LotId)
            throw new DomainException("One lot per wash — every item must come from " + LotSerial + ".");
        if (_lines.Count >= MaxLines)
            throw new DomainException($"A wash can hold at most {MaxLines} lines.");
        if (_lines.Any(l => l.StockBalanceId == source.Id))
            throw new DomainException($"{source.ItemName} ({source.Grade}) is already on this wash.");

        var line = new WashLine(Id, source, category, pieces, pricePerPiece);
        _lines.Add(line);
        return line;
    }

    /// <summary>Records what has come back so far for one line (cumulative, not incremental) and
    /// returns the change since the last save, which the caller pushes through the stock ledger.</summary>
    public WashReceiveDelta Receive(Guid lineId, int backA, int backC, int rejected, decimal pricePerPiece, string category, DateOnly today)
    {
        EnsureNotFinalized();
        var line = _lines.FirstOrDefault(l => l.Id == lineId) ?? throw new DomainException("That line is not on this wash.");
        var delta = line.Receive(backA, backC, rejected, pricePerPiece, category);
        ReceivedDate = today;
        RefreshStatus();
        return delta;
    }

    public void SetPaymentStatus(WashPaymentStatus status) => PaymentStatus = status;

    public void FinalizeForPayment(Guid userId, DateTimeOffset now)
    {
        EnsureNotFinalized();
        if (StillOut > 0)
            throw new DomainException($"{StillOut:N0} pieces are still at the washer — receive the full wash before finalizing for payment.");

        Status = WashStatus.Finalized;
        FinalizedAt = now;
        FinalizedById = userId;
    }

    public void Reopen()
    {
        if (Status != WashStatus.Finalized)
            throw new DomainException("Only a finalized wash can be reopened.");
        if (PaymentStatus == WashPaymentStatus.Paid)
            throw new DomainException("This wash is already paid — mark it unpaid first if the bill really has to change.");

        FinalizedAt = null;
        FinalizedById = null;
        RefreshStatus();
    }

    /// <summary>A wash can only be cancelled while nothing has come back — after that the stock
    /// history depends on it.</summary>
    public void EnsureCancellable()
    {
        if (_lines.Any(l => l.BackA + l.BackC + l.Rejected > 0))
            throw new DomainException("Pieces from this wash were already received — set them back to 0 before deleting it.");
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }

    private void RefreshStatus()
    {
        var back = _lines.Sum(l => l.BackA + l.BackC + l.Rejected);
        Status = back == 0 ? WashStatus.Sent : StillOut > 0 ? WashStatus.Partial : WashStatus.Received;
    }

    private void EnsureNotFinalized()
    {
        if (Status == WashStatus.Finalized)
            throw new DomainException("This wash is finalized for payment — reopen it to make changes.");
    }
}

public sealed record WashReceiveDelta(WashLine Line, int DeltaA, int DeltaC, int DeltaRejected);

public class WashLine : Entity
{
    public Guid WashBatchId { get; private set; }
    public Guid StockBalanceId { get; private set; }
    public Guid LotItemId { get; private set; }
    public string ItemName { get; private set; } = default!;

    /// <summary>The grade the pieces left as.</summary>
    public Grade Grade { get; private set; }

    public string Category { get; private set; } = default!;
    public int Sent { get; private set; }
    public decimal PricePerPiece { get; private set; }
    public int BackA { get; private set; }
    public int BackC { get; private set; }
    public int Rejected { get; private set; }

    private WashLine()
    {
    }

    internal WashLine(Guid batchId, StockBalance source, string category, int pieces, decimal pricePerPiece)
    {
        if (pieces <= 0)
            throw new DomainException("Send at least one piece.");

        WashBatchId = batchId;
        StockBalanceId = source.Id;
        LotItemId = source.LotItemId;
        ItemName = source.ItemName;
        Grade = source.Grade;
        Sent = pieces;
        SetPricing(pricePerPiece, category);
    }

    public int StillOut => Sent - BackA - BackC - Rejected;

    public int Good => BackA + BackC;

    public decimal AmountDue => Good * PricePerPiece;

    internal WashReceiveDelta Receive(int backA, int backC, int rejected, decimal pricePerPiece, string category)
    {
        if (backA < 0 || backC < 0 || rejected < 0)
            throw new DomainException("Received counts cannot be negative.");
        if ((long)backA + backC + rejected > Sent)
            throw new DomainException($"{ItemName}: A + C + rejected ({backA + backC + rejected:N0}) is more than the {Sent:N0} sent.");

        SetPricing(pricePerPiece, category);
        var delta = new WashReceiveDelta(this, backA - BackA, backC - BackC, rejected - Rejected);
        BackA = backA;
        BackC = backC;
        Rejected = rejected;
        return delta;
    }

    private void SetPricing(decimal pricePerPiece, string category)
    {
        if (pricePerPiece is < 0 or > 100_000)
            throw new DomainException("Wash price must be between 0 and 100,000 per piece.");
        if (!WashCategories.IsValid(category))
            throw new DomainException($"Unknown wash category '{category}'.");

        PricePerPiece = pricePerPiece;
        Category = category;
    }
}
