using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A currency's conversion rate to USD at a point in time (the live schema's exchange_rates
/// table). USD is the platform's base currency throughout — see Money.Usd().
/// </summary>
public class ExchangeRate : BaseEntity, IAggregateRoot
{
    public string CurrencyCode { get; private set; } = default!;
    public decimal RateToUsd { get; private set; }
    public DateTimeOffset EffectiveAt { get; private set; }

    private ExchangeRate()
    {
        // EF Core
    }

    public ExchangeRate(string currencyCode, decimal rateToUsd, DateTimeOffset effectiveAt)
    {
        if (rateToUsd <= 0)
            throw new DomainException("Exchange rate must be greater than zero.");

        CurrencyCode = currencyCode;
        RateToUsd = rateToUsd;
        EffectiveAt = effectiveAt;
    }
}
