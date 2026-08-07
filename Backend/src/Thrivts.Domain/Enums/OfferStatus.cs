namespace Thrivts.Domain.Enums;

/// <summary>
/// Status of an admin-initiated direct offer to a seller (requirement_seller_offers).
/// Accepting an offer does not itself create a deal — admin still runs the deal-from-match
/// step separately (see THRIVTS_FLOW_MATRIX.md section 5).
/// </summary>
public enum OfferStatus
{
    Pending,
    Countered,
    Accepted,
    Declined
}
