using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Inventory;

public enum Grade
{
    A,
    C,
}

public enum StockMovementType
{
    CosignIn,
    ContainerDispatch,
    SmallOrderDispatch,
    DispatchReversal,
    SkuShipment,
    LooseDispatch,
    LotRemoval,

    /// <summary>The owner corrected a finalized lot's numbers.</summary>
    Correction,

    /// <summary>Pieces left for the washer (out of OnHand into AtWash).</summary>
    WashOut,

    /// <summary>Washed pieces came back in the grade they left as.</summary>
    WashReturn,

    /// <summary>Washed pieces came back in a different grade and landed in this balance.</summary>
    WashRegrade,
}

/// <summary>
/// Finalized stock for one (lot, item, grade). Created at co-sign. Running counters let every
/// inventory screen read balances directly instead of re-summing history, so the cost of a page
/// stays flat however many movements accumulate.
///
/// OnHand counts every finalized piece still in the warehouse, including pieces packed into sacks;
/// InSacks is the part of OnHand committed to live sacks. Anything that takes stock — packing,
/// container or small-order dispatch, loose dispatch — draws only from Available, which is why the
/// same pieces can no longer leave twice. All counter changes are made with conditional atomic
/// UPDATEs in the persistence layer (never read-modify-write), so concurrent devices are safe.
///
/// Washing: pieces at the washer leave OnHand for AtWash. When they come back in the same grade they
/// return to OnHand; in a different grade they land in the other grade's balance (its WashIn) and
/// count as WashLost here, as do pieces the wash ruined. So for every balance:
///   QuantityIn + WashIn = OnHand + AtWash + Dispatched(Container+Small+Sku) + WashLost.
/// </summary>
public class StockBalance : Entity
{
    public Guid LotId { get; private set; }
    public Guid LotItemId { get; private set; }
    public Guid? ItemId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public DateOnly ArrivalDate { get; private set; }
    public string ItemName { get; private set; } = default!;
    public string NormalizedItemName { get; private set; } = default!;
    public Grade Grade { get; private set; }
    public int QuantityIn { get; private set; }
    public int OnHand { get; private set; }
    public int InSacks { get; private set; }
    public int DispatchedContainer { get; private set; }
    public int DispatchedSmall { get; private set; }
    public int DispatchedSku { get; private set; }
    public int AtWash { get; private set; }
    public int WashIn { get; private set; }
    public int WashLost { get; private set; }
    public DateTimeOffset FinalizedAt { get; private set; }

    private StockBalance()
    {
    }

    /// <summary>An empty balance for a grade the lot item didn't have at co-sign, opened because
    /// washed pieces came back in that grade (or the owner corrected the numbers into it).</summary>
    public static StockBalance OpenEmpty(StockBalance sibling, Grade grade) => new()
    {
        LotId = sibling.LotId,
        LotItemId = sibling.LotItemId,
        ItemId = sibling.ItemId,
        LotSerial = sibling.LotSerial,
        SupplierId = sibling.SupplierId,
        SupplierName = sibling.SupplierName,
        ArrivalDate = sibling.ArrivalDate,
        ItemName = sibling.ItemName,
        NormalizedItemName = sibling.NormalizedItemName,
        Grade = grade,
        FinalizedAt = sibling.FinalizedAt,
    };

    /// <summary>True when nothing has happened to this stock since co-sign — no dispatch, sack or wash.</summary>
    public bool IsUntouched => OnHand == QuantityIn && InSacks == 0 && AtWash == 0 && WashIn == 0 && WashLost == 0
        && DispatchedContainer == 0 && DispatchedSmall == 0 && DispatchedSku == 0;

    public StockBalance(
        Guid lotId, Guid lotItemId, Guid? itemId, string lotSerial, Guid supplierId, string supplierName,
        DateOnly arrivalDate, string itemName, Grade grade, int quantity, DateTimeOffset finalizedAt)
    {
        if (quantity <= 0)
            throw new DomainException("A stock balance needs a positive quantity.");

        LotId = lotId;
        LotItemId = lotItemId;
        ItemId = itemId;
        LotSerial = lotSerial;
        SupplierId = supplierId;
        SupplierName = supplierName;
        ArrivalDate = arrivalDate;
        ItemName = itemName;
        NormalizedItemName = Text.Normalize(itemName);
        Grade = grade;
        QuantityIn = quantity;
        OnHand = quantity;
        FinalizedAt = finalizedAt;
    }

    public int Available => OnHand - InSacks;
}

/// <summary>Append-only history of every change to OnHand. Never updated or deleted.</summary>
public class StockMovement : Entity
{
    public Guid StockBalanceId { get; private set; }
    public Guid LotId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public string ItemName { get; private set; } = default!;
    public Grade Grade { get; private set; }
    public int Quantity { get; private set; }
    public StockMovementType Type { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? UserId { get; private set; }

    private StockMovement()
    {
    }

    public StockMovement(
        StockBalance balance, int quantity, StockMovementType type, Guid? referenceId, string? referenceNumber,
        DateOnly businessDate, DateTimeOffset occurredAt, Guid? userId)
    {
        StockBalanceId = balance.Id;
        LotId = balance.LotId;
        LotSerial = balance.LotSerial;
        ItemName = balance.ItemName;
        Grade = balance.Grade;
        Quantity = quantity;
        Type = type;
        ReferenceId = referenceId;
        ReferenceNumber = referenceNumber;
        BusinessDate = businessDate;
        OccurredAt = occurredAt;
        UserId = userId;
    }
}

public enum DispatchType
{
    Container,
    SmallOrder,
}

/// <summary>A container (VEWC##) or small-order (VEWO##) shipment drawn from specific lots.</summary>
public class Dispatch : AuditableEntity, ISoftDeletable, IVersioned
{
    public const int MaxLines = 500;

    private readonly List<DispatchLine> _lines = [];

    public string Number { get; private set; } = default!;
    public DispatchType Type { get; private set; }
    public DateOnly DispatchDate { get; private set; }
    public DateOnly? OrderReceivedDate { get; private set; }
    public Guid? ClientOrderId { get; private set; }
    public string? ClientName { get; private set; }
    public string? DestinationCountry { get; private set; }
    public string? ContainerNumber { get; private set; }
    public int? TotalBoxes { get; private set; }
    public decimal? TotalWeightKg { get; private set; }
    public string? Notes { get; private set; }
    public int TotalPieces { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyList<DispatchLine> Lines => _lines;

    private Dispatch()
    {
    }

    public Dispatch(string number, DispatchType type)
    {
        Number = number;
        Type = type;
    }

    public void SetDetails(
        DateOnly dispatchDate, DateOnly? orderReceivedDate, Guid? clientOrderId, string? clientName,
        string? destinationCountry, string? containerNumber, int? totalBoxes, decimal? totalWeightKg, string? notes)
    {
        if (totalBoxes is < 0 || totalWeightKg is < 0)
            throw new DomainException("Boxes and weight cannot be negative.");

        DispatchDate = dispatchDate;
        OrderReceivedDate = orderReceivedDate;
        ClientOrderId = clientOrderId;
        ClientName = clientName;
        DestinationCountry = destinationCountry;
        ContainerNumber = Type == DispatchType.Container ? containerNumber : null;
        TotalBoxes = totalBoxes;
        TotalWeightKg = totalWeightKg;
        Notes = notes;
    }

    public void AddLine(StockBalance balance, int quantity, string? notes)
    {
        if (quantity <= 0)
            throw new DomainException("Line quantity must be positive.");
        if (_lines.Count >= MaxLines)
            throw new DomainException($"A dispatch can hold at most {MaxLines} lines.");

        _lines.Add(new DispatchLine(Id, balance, quantity, notes));
        TotalPieces += quantity;
    }

    public void ClearLines()
    {
        _lines.Clear();
        TotalPieces = 0;
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}

public class DispatchLine : Entity
{
    public Guid DispatchId { get; private set; }
    public Guid StockBalanceId { get; private set; }
    public Guid LotId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public string ItemName { get; private set; } = default!;
    public Grade Grade { get; private set; }
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }

    private DispatchLine()
    {
    }

    internal DispatchLine(Guid dispatchId, StockBalance balance, int quantity, string? notes)
    {
        DispatchId = dispatchId;
        StockBalanceId = balance.Id;
        LotId = balance.LotId;
        LotSerial = balance.LotSerial;
        ItemName = balance.ItemName;
        Grade = balance.Grade;
        Quantity = quantity;
        Notes = notes;
    }
}

public enum ReturnReason
{
    Defective,
    Damaged,
    Stained,
    WrongItem,
    SizeIssue,
    Other,
}

/// <summary>Rejected pieces sent back to a supplier against the supplier's slip number.</summary>
public class RejectionReturn : AuditableEntity, ISoftDeletable
{
    private readonly List<RejectionReturnLine> _lines = [];

    public string SlipNumber { get; private set; } = default!;
    public DateOnly BusinessDate { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public int Sacks { get; private set; }
    public ReturnReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public int TotalPieces { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    public IReadOnlyList<RejectionReturnLine> Lines => _lines;

    private RejectionReturn()
    {
    }

    public RejectionReturn(string slipNumber, DateOnly businessDate, Guid supplierId, string supplierName, int sacks, ReturnReason reason, string? notes)
    {
        if (string.IsNullOrWhiteSpace(slipNumber))
            throw new DomainException("Enter the slip number.");
        if (sacks < 0)
            throw new DomainException("Sacks cannot be negative.");

        SlipNumber = slipNumber.Trim().ToUpperInvariant();
        BusinessDate = businessDate;
        SupplierId = supplierId;
        SupplierName = supplierName;
        Sacks = sacks;
        Reason = reason;
        Notes = notes;
    }

    public void AddLine(Guid lotItemId, string lotSerial, string itemName, int quantity)
    {
        _lines.Add(new RejectionReturnLine(Id, lotItemId, lotSerial, itemName, quantity));
        TotalPieces += quantity;
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}

public class RejectionReturnLine : Entity
{
    public Guid RejectionReturnId { get; private set; }
    public Guid LotItemId { get; private set; }
    public string LotSerial { get; private set; } = default!;
    public string ItemName { get; private set; } = default!;
    public int Quantity { get; private set; }

    private RejectionReturnLine()
    {
    }

    internal RejectionReturnLine(Guid returnId, Guid lotItemId, string lotSerial, string itemName, int quantity)
    {
        RejectionReturnId = returnId;
        LotItemId = lotItemId;
        LotSerial = lotSerial;
        ItemName = itemName;
        Quantity = quantity;
    }
}
