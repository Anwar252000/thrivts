namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's currency_type enum exactly. Member names are the literal
/// DB labels (uppercase currency codes) so the default string conversion needs no remapping.</summary>
public enum CurrencyType
{
    USD,
    GBP,
    EUR,
    PKR
}
