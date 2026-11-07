using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Lots;

public enum LotStage
{
    Counting,
    Grading,
    AwaitingCosign,
    Finalized,
}

public enum RejectionReason
{
    PakkaDaagh,
    KataPhataFake,
    WrongStock,
}

/// <summary>
/// One gate arrival (a VEWL serial). Lifecycle: Ali logs it at the gate → Burhan counts received
/// pieces → finalizes the count → grades every item A/B/C/rejected → signs → the inventory manager
/// co-signs, which turns it into finalized stock. The serial is a display label only; identity is
/// the UUID, so a reused serial can never collide with an old lot.
/// </summary>
public class Lot : AuditableEntity, ISoftDeletable, IVersioned
{
    public const int MaxItems = 200;
    public const int MaxPiecesPerLine = 1_000_000;
    public const int PiecesPerGradingDay = 1000;

    private readonly List<LotItem> _items = [];

    public string Serial { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public int Sacks { get; private set; }
    public DateOnly ArrivalDate { get; private set; }
    public Guid GatedById { get; private set; }
    public DateTimeOffset GatedAt { get; private set; }
    public LotStage Stage { get; private set; } = LotStage.Counting;
    public DateTimeOffset? CountFinalizedAt { get; private set; }
    public DateTimeOffset? GradeDraftSavedAt { get; private set; }
    public DateTimeOffset? SupervisorSignedAt { get; private set; }
    public Guid? SupervisorSignedById { get; private set; }
    public Guid? SupervisorSignatureId { get; private set; }
    public DateTimeOffset? CosignedAt { get; private set; }
    public Guid? CosignedById { get; private set; }
    public Guid? CosignSignatureId { get; private set; }
    public DateTimeOffset? SentBackAt { get; private set; }
    public Guid? SentBackById { get; private set; }
    public string? SentBackReason { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyList<LotItem> Items => _items;

    private Lot()
    {
    }

    public Lot(string serial, Guid supplierId, string supplierName, int sacks, DateOnly arrivalDate, Guid gatedById, DateTimeOffset gatedAt)
    {
        if (sacks is < 1 or > 10_000)
            throw new DomainException("Enter the number of sacks (1–10,000).");

        Serial = serial;
        SupplierId = supplierId;
        SupplierName = supplierName;
        Sacks = sacks;
        ArrivalDate = arrivalDate;
        GatedById = gatedById;
        GatedAt = gatedAt;
    }

    public int TotalReceived => _items.Sum(i => i.Received);

    public DateTimeOffset CountDueAt => GatedAt.AddHours(24);

    /// <summary>Grading window = 1 day per 1,000 pieces received (minimum 1 day), counted from
    /// the moment the count was finalized.</summary>
    public int GradingDays => Math.Max(1, (int)Math.Ceiling(TotalReceived / (double)PiecesPerGradingDay));

    public DateTimeOffset? GradeDueAt => CountFinalizedAt?.AddDays(GradingDays);

    public LotItem AddItem(Guid? itemId, string itemName, int received)
    {
        EnsureStage(LotStage.Counting, "Items can only be added while counting.");
        if (_items.Count >= MaxItems)
            throw new DomainException($"A lot can hold at most {MaxItems} items.");

        var name = Text.Clean(itemName);
        if (name.Length == 0)
            throw new DomainException("Item name is required.");
        if (_items.Any(i => i.NormalizedItemName == Text.Normalize(name)))
            throw new DomainException("That item is already in the lot.");

        var line = new LotItem(Id, itemId, name, received, _items.Count);
        _items.Add(line);
        return line;
    }

    public void SetReceived(Guid lineId, int received)
    {
        EnsureStage(LotStage.Counting, "Received counts can only change while counting — use Fix count first.");
        Line(lineId).SetReceived(received);
    }

    public void RemoveItem(Guid lineId)
    {
        EnsureStage(LotStage.Counting, "Items can only be removed while counting.");
        _items.Remove(Line(lineId));
        for (var i = 0; i < _items.Count; i++)
            _items[i].SetSortOrder(i);
    }

    public void FinalizeCount(DateTimeOffset now)
    {
        EnsureStage(LotStage.Counting, "The count is already finalized.");
        if (_items.Count == 0)
            throw new DomainException("Add at least one item before finalizing the count.");
        if (_items.Any(i => i.Received <= 0))
            throw new DomainException("Every item needs a received count above 0 before you can finalize.");

        Stage = LotStage.Grading;
        CountFinalizedAt = now;
    }

    /// <summary>"Fix count": back to counting. Grades already entered are kept.</summary>
    public void ReopenCount()
    {
        EnsureStage(LotStage.Grading, "Only a lot that is being graded can go back to counting.");
        Stage = LotStage.Counting;
        CountFinalizedAt = null;
    }

    public void SetGrades(Guid lineId, int a, int b, int c, int rejected, RejectionReason? reason)
    {
        EnsureStage(LotStage.Grading, "Grades can only change while grading.");
        Line(lineId).SetGrades(a, b, c, rejected, reason);
    }

    public void SaveGradingDraft(DateTimeOffset now)
    {
        EnsureStage(LotStage.Grading, "Only a lot that is being graded has a draft.");
        GradeDraftSavedAt = now;
    }

    /// <summary>Supervisor signs and finalizes. B is the "needs washing" grade and must be moved
    /// to A, C or rejected before signing; every piece received must be accounted for, so no
    /// pieces silently vanish or appear between the count and the stock.</summary>
    public void SignBySupervisor(Guid userId, Guid signatureId, DateTimeOffset now)
    {
        EnsureStage(LotStage.Grading, "Only a lot that is being graded can be signed.");
        if (_items.Count == 0 || TotalReceived <= 0)
            throw new DomainException("Add items and received counts before signing.");
        if (_items.Any(i => i.QtyB > 0))
            throw new DomainException("Grade B must be 0 — move it to A, C or rejections before signing.");

        var unbalanced = _items.FirstOrDefault(i => i.Unaccounted != 0);
        if (unbalanced is not null)
            throw new DomainException(unbalanced.Unaccounted > 0
                ? $"{unbalanced.ItemName}: {unbalanced.Unaccounted} pieces are not graded yet."
                : $"{unbalanced.ItemName}: graded {-unbalanced.Unaccounted} more pieces than were received.");

        var missingReason = _items.FirstOrDefault(i => i.Rejected > 0 && i.RejectionReason is null);
        if (missingReason is not null)
            throw new DomainException($"{missingReason.ItemName}: choose a rejection reason.");

        Stage = LotStage.AwaitingCosign;
        SupervisorSignedAt = now;
        SupervisorSignedById = userId;
        SupervisorSignatureId = signatureId;
        SentBackAt = null;
        SentBackById = null;
        SentBackReason = null;
    }

    /// <summary>The supervisor takes back their own signature to edit again.</summary>
    public void UnlockSignature()
    {
        EnsureStage(LotStage.AwaitingCosign, "Only a signed lot can be unlocked.");
        ClearSupervisorSignature();
    }

    public void SendBack(Guid userId, string? reason, DateTimeOffset now)
    {
        EnsureStage(LotStage.AwaitingCosign, "Only a lot awaiting co-sign can be sent back.");
        ClearSupervisorSignature();
        SentBackAt = now;
        SentBackById = userId;
        SentBackReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void Cosign(Guid userId, Guid signatureId, DateTimeOffset now)
    {
        EnsureStage(LotStage.AwaitingCosign, "Only a lot signed by the supervisor can be co-signed.");
        Stage = LotStage.Finalized;
        CosignedAt = now;
        CosignedById = userId;
        CosignSignatureId = signatureId;
    }

    /// <summary>
    /// Owner correction of a finalized line (a miscount found later). The grading record is
    /// rewritten and the received count follows it, so the line stays balanced; the caller moves
    /// stock by the returned deltas through the ledger. Rejections already sent back to the supplier
    /// can't be corrected away.
    /// </summary>
    public (int DeltaA, int DeltaC) CorrectFinalizedItem(Guid lineId, int a, int c, int rejected, RejectionReason? reason)
    {
        EnsureStage(LotStage.Finalized, "Only a finalized lot can be corrected — before that, edit the grading.");
        var line = Line(lineId);
        var effectiveReason = rejected > 0 ? reason ?? line.RejectionReason : null;
        if (rejected > 0 && effectiveReason is null)
            throw new DomainException($"{line.ItemName}: choose a rejection reason.");
        if (rejected + line.WashRejected < line.ReturnedRejected)
            throw new DomainException($"{line.ItemName}: {line.ReturnedRejected} rejected pieces were already returned to the supplier.");
        if (a + c + rejected == 0)
            throw new DomainException($"{line.ItemName}: a line can't be corrected to zero — remove the item instead.");

        var delta = (a - line.QtyA, c - line.QtyC);
        line.SetGrades(a, 0, c, rejected, effectiveReason);
        line.SetReceived(a + c + rejected);
        return delta;
    }

    /// <summary>Removes an item from a finalized lot (it was never really there). The caller must
    /// first make sure none of its stock has moved.</summary>
    public LotItem RemoveFinalizedItem(Guid lineId)
    {
        EnsureStage(LotStage.Finalized, "Only a finalized lot's items are removed this way.");
        var line = Line(lineId);
        if (_items.Count == 1)
            throw new DomainException("This is the lot's only item — delete the whole lot instead.");
        if (line.ReturnedRejected > 0)
            throw new DomainException($"{line.ItemName}: its rejections were already returned to the supplier.");

        _items.Remove(line);
        for (var i = 0; i < _items.Count; i++)
            _items[i].SetSortOrder(i);
        return line;
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }

    private void ClearSupervisorSignature()
    {
        Stage = LotStage.Grading;
        SupervisorSignedAt = null;
        SupervisorSignedById = null;
        SupervisorSignatureId = null;
    }

    private LotItem Line(Guid lineId) =>
        _items.FirstOrDefault(i => i.Id == lineId) ?? throw new DomainException("That item is not in this lot.");

    private void EnsureStage(LotStage expected, string message)
    {
        if (Stage != expected)
            throw new DomainException(message);
    }
}

public class LotItem : Entity
{
    public Guid LotId { get; private set; }
    public Guid? ItemId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public string NormalizedItemName { get; private set; } = default!;
    public int Received { get; private set; }
    public int QtyA { get; private set; }
    public int QtyB { get; private set; }
    public int QtyC { get; private set; }
    public int Rejected { get; private set; }
    public RejectionReason? RejectionReason { get; private set; }
    public int ReturnedRejected { get; private set; }

    /// <summary>Pieces from this line that the washer ruined. They become rejections owed back to
    /// the supplier, on top of the ones found at grading (which stay as graded).</summary>
    public int WashRejected { get; private set; }

    public int SortOrder { get; private set; }

    private LotItem()
    {
    }

    internal LotItem(Guid lotId, Guid? itemId, string itemName, int received, int sortOrder)
    {
        LotId = lotId;
        ItemId = itemId;
        ItemName = itemName;
        NormalizedItemName = Text.Normalize(itemName);
        SortOrder = sortOrder;
        SetReceived(received);
    }

    public int Graded => QtyA + QtyB + QtyC + Rejected;

    public int Unaccounted => Received - Graded;

    public int Net => QtyA + QtyC;

    public int TotalRejected => Rejected + WashRejected;

    public int PendingRejections => TotalRejected - ReturnedRejected;

    /// <summary>Records (or, with a negative delta, corrects) pieces rejected after washing.</summary>
    public void ApplyWashRejected(int delta)
    {
        if (WashRejected + delta < 0)
            throw new DomainException($"{ItemName}: wash rejections can't go below zero.");
        if (TotalRejected + delta < ReturnedRejected)
            throw new DomainException($"{ItemName}: those rejected pieces were already returned to the supplier.");

        WashRejected += delta;
    }

    internal void SetReceived(int received)
    {
        if (received is < 0 or > Lot.MaxPiecesPerLine)
            throw new DomainException($"Received pieces must be between 0 and {Lot.MaxPiecesPerLine:N0}.");

        Received = received;
    }

    internal void SetGrades(int a, int b, int c, int rejected, RejectionReason? reason)
    {
        if (a < 0 || b < 0 || c < 0 || rejected < 0)
            throw new DomainException("Grade counts cannot be negative.");
        if ((long)a + b + c + rejected > Lot.MaxPiecesPerLine)
            throw new DomainException("Grade counts are too large.");

        QtyA = a;
        QtyB = b;
        QtyC = c;
        Rejected = rejected;
        RejectionReason = rejected > 0 ? reason : null;
    }

    internal void SetSortOrder(int order) => SortOrder = order;

    /// <summary>Rejected pieces sent back to the supplier. Never touches net stock — rejected
    /// pieces were never part of it.</summary>
    public void RecordReturn(int pieces)
    {
        if (pieces <= 0)
            throw new DomainException("Return quantity must be positive.");
        if (pieces > PendingRejections)
            throw new DomainException($"{ItemName}: only {PendingRejections} rejected pieces are waiting to go back.");

        ReturnedRejected += pieces;
    }

    public void ReverseReturn(int pieces) => ReturnedRejected = Math.Max(0, ReturnedRejected - pieces);
}

/// <summary>Rejected sacks leaving through the gate back to a supplier (GATE-### serial).</summary>
public class GateExit : AuditableEntity, ISoftDeletable
{
    public string Serial { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public int Sacks { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    private GateExit()
    {
    }

    public GateExit(string serial, Guid supplierId, string supplierName, int sacks, DateOnly businessDate)
    {
        if (sacks is < 1 or > 10_000)
            throw new DomainException("Enter the number of sacks (1–10,000).");

        Serial = serial;
        SupplierId = supplierId;
        SupplierName = supplierName;
        Sacks = sacks;
        BusinessDate = businessDate;
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}
