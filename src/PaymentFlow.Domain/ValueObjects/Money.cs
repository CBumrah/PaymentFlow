using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount in a specific currency. Wrapping decimal +
/// currency together prevents an entire class of bugs — e.g., silently
/// adding USD cents to EUR cents, or allowing negative payment amounts
/// through unvalidated raw decimals.
/// </summary>
public sealed class Money : ValueObject
{
    // Minor units (cents) avoid floating-point/decimal rounding issues
    // that arise from repeated arithmetic on fractional currency amounts.
    public long AmountInMinorUnits { get; }
    public string Currency { get; }

    private Money(long amountInMinorUnits, string currency)
    {
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
    }

    /// <summary>
    /// Creates Money from a decimal amount (e.g., 19.99) in the given
    /// ISO 4217 currency code (e.g., "USD"). Assumes 2 decimal places —
    /// currencies with different minor-unit precision (e.g., JPY) would
    /// need an extended factory in a later episode.
    /// </summary>
    public static Money FromDecimal(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        }

        var minorUnits = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        return new Money(minorUnits, currency.ToUpperInvariant());
    }

    public static Money FromMinorUnits(long amountInMinorUnits, string currency)
    {
        if (amountInMinorUnits < 0)
        {
            throw new ArgumentException("Money amount cannot be negative.", nameof(amountInMinorUnits));
        }

        return new Money(amountInMinorUnits, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => FromMinorUnits(0, currency);

    public decimal AsDecimal() => AmountInMinorUnits / 100m;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(AmountInMinorUnits + other.AmountInMinorUnits, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        var result = AmountInMinorUnits - other.AmountInMinorUnits;
        if (result < 0)
        {
            throw new InvalidOperationException("Resulting amount cannot be negative.");
        }

        return new Money(result, Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return AmountInMinorUnits > other.AmountInMinorUnits;
    }

    public bool IsGreaterThanOrEqualTo(Money other)
    {
        EnsureSameCurrency(other);
        return AmountInMinorUnits >= other.AmountInMinorUnits;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot operate on Money in different currencies: {Currency} vs {other.Currency}.");
        }
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AmountInMinorUnits;
        yield return Currency;
    }

    public override string ToString() => $"{AsDecimal():F2} {Currency}";
}