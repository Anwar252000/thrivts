using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A currency's conversion rate to USD at a point in time. NOTE: like Category, the live
/// schema's exchange_rates.id is a plain integer identity column, not a uuid — does not inherit
/// BaseEntity. USD is the platform's base currency throughout.
/// </summary>
public class ExchangeRate : IAggregateRoot
{
    public int Id { get; private set; }
    public CurrencyType Currency { get; private set; }
    public decimal RateToUsd { get; private set; }
    public decimal? UsdToRate { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? SetBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ExchangeRate()
    {
        // EF Core
    }

    public ExchangeRate(CurrencyType currency, decimal rateToUsd, Guid? setBy = null, string? notes = null)
    {
        if (rateToUsd <= 0)
            throw new DomainException("Exchange rate must be greater than zero.");

        Currency = currency;
        RateToUsd = rateToUsd;
        UsdToRate = rateToUsd == 0 ? null : 1 / rateToUsd;
        SetBy = setBy;
        Notes = notes;
    }
}
