using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// The live schema's purchase_orders table — one per Deal, auto-issued by a DB trigger
/// (trg_auto_issue_po / _auto_issue_po_for_deal) the moment a Deal is inserted as Confirmed. That
/// path never goes through EF (it's 100% trigger-driven — confirmed empirically against the live
/// DB), so most rows are read-only from this codebase's point of view; the constructor here only
/// covers the admin's manual "Issue PO" fallback (issue_purchase_order) for a Confirmed deal whose
/// auto-issue somehow didn't fire.
/// </summary>
public class PurchaseOrder : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public string PoNumber { get; private set; } = default!;
    public PoStatus Status { get; private set; } = PoStatus.Issued;
    public decimal AmountUsd { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateTimeOffset DueAt { get; private set; }

    public string? PaymentMethod { get; private set; }
    public string? PaymentLinkUrl { get; private set; }
    public DateTimeOffset? LinkRequestedAt { get; private set; }
    public string? ReceiptUrl { get; private set; }
    public DateTimeOffset? PaidMarkedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public Guid? IssuedBy { get; private set; }
    public string? Notes { get; private set; }

    private PurchaseOrder()
    {
        // EF Core
    }

    public PurchaseOrder(Guid dealId, string poNumber, decimal amountUsd, DateTimeOffset dueAt, Guid? issuedBy, string? notes = null)
    {
        DealId = dealId;
        PoNumber = poNumber;
        AmountUsd = amountUsd;
        DueAt = dueAt;
        IssuedBy = issuedBy;
        Notes = notes;
    }

    /// <summary>Buyer uploads a receipt (replaces po_mark_paid) — only from a still-open, no-receipt-yet PO.</summary>
    public void MarkPaymentSubmitted(string receiptUrl, DateTimeOffset occurredAt)
    {
        if (Status != PoStatus.Issued)
            throw new DomainException("No open PO awaiting payment.");

        ReceiptUrl = receiptUrl;
        PaidMarkedAt = occurredAt;
        Status = PoStatus.PaymentSubmitted;
        PaymentMethod ??= "bank_transfer";
    }

    /// <summary>Admin confirms payment — replaces BOTH admin_mark_po_paid (issued or
    /// payment_submitted -> verified, a one-step shortcut) and verify_purchase_order
    /// (payment_submitted -> verified, after reviewing a buyer's receipt). The live RPCs differ only
    /// in which source states they accept; there's no further domain distinction once verified, so
    /// one method serves both callers.</summary>
    public void MarkVerified(Guid? verifiedBy, DateTimeOffset occurredAt)
    {
        if (Status is not (PoStatus.Issued or PoStatus.PaymentSubmitted))
            throw new DomainException($"This PO cannot be verified from its current status ({Status}).");

        Status = PoStatus.Verified;
        VerifiedAt = occurredAt;
        VerifiedBy = verifiedBy;
        PaidMarkedAt ??= occurredAt;
    }

    /// <summary>Admin attaches a payment link (replaces po_add_payment_link) — only on a still-open PO.</summary>
    public void AddPaymentLink(string link)
    {
        if (Status != PoStatus.Issued)
            throw new DomainException("No open PO to attach a payment link to.");

        PaymentLinkUrl = link;
        PaymentMethod = "payment_link";
    }

    /// <summary>Buyer asks admin for a payment link (replaces po_request_payment_link) — only on a still-open PO.</summary>
    public void RequestPaymentLink(DateTimeOffset occurredAt)
    {
        if (Status != PoStatus.Issued)
            throw new DomainException("No open PO to request a payment link for.");

        PaymentMethod = "payment_link";
        LinkRequestedAt = occurredAt;
    }
}
