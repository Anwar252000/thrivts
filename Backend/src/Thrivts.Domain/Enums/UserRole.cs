namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's user_role enum exactly (admin, buyer, seller, agency).
/// Influencers are a separate table (influencers) not tied to a profiles.role value.</summary>
public enum UserRole
{
    Buyer,
    Seller,
    Agency,
    Admin
}
