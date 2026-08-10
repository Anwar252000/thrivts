namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's offer_status enum exactly (requirement_seller_offers.status
/// is plain text in the live DB, defaulting to 'sent', but this enum type matches the values used).</summary>
public enum OfferStatus
{
    Sent,
    Viewed,
    Accepted,
    Declined,
    Countered,
    Expired,
    Withdrawn
}
