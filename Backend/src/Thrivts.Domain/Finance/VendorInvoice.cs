using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Finance;

public enum InvoiceMode
{
    NonGraded,
    Final,
}

public enum InvoiceStatus
{
    Generated,
    Sent,
}

/// <summary>
/// Owner-only vendor invoice for one lot, at the owner's buy prices. Its total becomes the
/// supplier's hisab due. The total is always computed here from the lines — never accepted from
/// the client — so a tampered request cannot change what is owed.
/// </summary>
public class VendorInvoice : AuditableEntity, ISoftDeletable
{
    private readonly List<VendorInvoiceLine> _lines = [];
    private readonly List<InvoiceRejection> _rejections = [];

    public string Number { get; private set; } = default!;
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public Guid? LotId { get; private set; }
    public string? LotSerial { get; private set; }
    public InvoiceMode Mode { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Generated;
    public DateTimeOffset GeneratedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public decimal Total { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    public IReadOnlyList<VendorInvoiceLine> Lines => _lines;
    public IReadOnlyList<InvoiceRejection> Rejections => _rejections;

    private VendorInvoice()
    {
    }

    public VendorInvoice(string number, Guid supplierId, string supplierName, Guid? lotId, string? lotSerial, InvoiceMode mode, DateTimeOffset generatedAt)
    {
        Number = number;
        SupplierId = supplierId;
        SupplierName = supplierName;
        LotId = lotId;
        LotSerial = lotSerial;
        Mode = mode;
        GeneratedAt = generatedAt;
    }

    public void AddLine(string itemName, int pieces, decimal buyPrice, IReadOnlyList<string>? mergedFrom)
    {
        if (pieces < 0 || buyPrice < 0)
            throw new DomainException("Pieces and prices cannot be negative.");

        var line = new VendorInvoiceLine(Id, itemName, pieces, buyPrice, isAcceptedRejection: false, mergedFrom);
        _lines.Add(line);
        Total += line.LineTotal;
    }

    /// <summary>Rejected pieces the owner agrees to keep and pay for are added as their own charge
    /// line and leave the rejection count — only the remainder goes back to the vendor.</summary>
    public void AddRejection(string itemName, int rejected, string? reason, int accepted, decimal acceptPrice)
    {
        if (rejected < 0 || accepted < 0 || acceptPrice < 0)
            throw new DomainException("Rejection values cannot be negative.");
        if (accepted > rejected)
            throw new DomainException($"{itemName}: cannot accept more than the {rejected} rejected pieces.");

        _rejections.Add(new InvoiceRejection(Id, itemName, rejected, reason, accepted, acceptPrice));

        if (accepted > 0 && acceptPrice > 0)
        {
            var line = new VendorInvoiceLine(Id, $"{itemName} (accepted rejections)", accepted, acceptPrice, isAcceptedRejection: true, null);
            _lines.Add(line);
            Total += line.LineTotal;
        }
    }

    public void MarkSent(DateTimeOffset at)
    {
        Status = InvoiceStatus.Sent;
        SentAt = at;
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}

public class VendorInvoiceLine : Entity
{
    public Guid VendorInvoiceId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public int Pieces { get; private set; }
    public decimal BuyPrice { get; private set; }
    public decimal LineTotal { get; private set; }
    public bool IsAcceptedRejection { get; private set; }
    public List<string>? MergedFrom { get; private set; }

    private VendorInvoiceLine()
    {
    }

    internal VendorInvoiceLine(Guid invoiceId, string itemName, int pieces, decimal buyPrice, bool isAcceptedRejection, IReadOnlyList<string>? mergedFrom)
    {
        VendorInvoiceId = invoiceId;
        ItemName = itemName;
        Pieces = pieces;
        BuyPrice = buyPrice;
        LineTotal = Math.Round(pieces * buyPrice, 2, MidpointRounding.AwayFromZero);
        IsAcceptedRejection = isAcceptedRejection;
        MergedFrom = mergedFrom?.ToList();
    }
}

public class InvoiceRejection : Entity
{
    public Guid VendorInvoiceId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public int Rejected { get; private set; }
    public string? Reason { get; private set; }
    public int Accepted { get; private set; }
    public decimal AcceptPrice { get; private set; }

    private InvoiceRejection()
    {
    }

    internal InvoiceRejection(Guid invoiceId, string itemName, int rejected, string? reason, int accepted, decimal acceptPrice)
    {
        VendorInvoiceId = invoiceId;
        ItemName = itemName;
        Rejected = rejected;
        Reason = reason;
        Accepted = accepted;
        AcceptPrice = acceptPrice;
    }

    public int Returned => Rejected - Accepted;
}

public enum PaymentMethod
{
    Bank,
    Cash,
    Hundi,
}

public class VendorPayment : AuditableEntity, ISoftDeletable
{
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public Guid? VendorInvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateOnly PaidOn { get; private set; }
    public string? Note { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    private VendorPayment()
    {
    }

    public VendorPayment(Guid supplierId, string supplierName, Guid? invoiceId, decimal amount, PaymentMethod method, DateOnly paidOn, string? note)
    {
        if (amount is <= 0 or > 1_000_000_000)
            throw new DomainException("Enter a valid payment amount.");

        SupplierId = supplierId;
        SupplierName = supplierName;
        VendorInvoiceId = invoiceId;
        Amount = amount;
        Method = method;
        PaidOn = paidOn;
        Note = note;
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}
