namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's po_status enum exactly.</summary>
public enum PoStatus
{
    Issued,
    PaymentSubmitted,
    Verified,
    Expired,
    Cancelled
}
