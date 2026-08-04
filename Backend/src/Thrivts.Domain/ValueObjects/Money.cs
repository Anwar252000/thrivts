namespace Thrivts.Domain.ValueObjects;

/// <summary>
/// Currency-safe amount. Always carries its currency explicitly — prevents the kind of
/// currency-mismatch bug (GBP vs USD) found during the pre-migration audit.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Usd(decimal amount) => new(amount, "USD");

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} to {b.Currency}.");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
