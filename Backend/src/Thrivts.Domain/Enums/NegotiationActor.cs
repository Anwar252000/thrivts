namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors seller_responses.last_actor ('buyer' | 'seller') — who made the most recent move
/// in a negotiation round. Also reused by OfferRound for the admin-offer negotiation loop.
/// </summary>
public enum NegotiationActor
{
    Buyer,
    Seller,
    Admin
}
