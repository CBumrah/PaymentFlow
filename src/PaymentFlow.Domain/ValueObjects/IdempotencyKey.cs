using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Domain.ValueObjects;

/// <summary>
/// Represents a client-supplied idempotency key used to prevent duplicate
/// payment creation on retry (e.g., a customer double-clicking "Pay" or a
/// client retrying after a timeout). The key is opaque to the domain — its
/// only job is to be compared for equality and validated for shape.
/// </summary>
public sealed class IdempotencyKey : ValueObject
{
    private const int MaxLength = 128;

    public string Value { get; }

    private IdempotencyKey(string value)
    {
        Value = value;
    }

    public static IdempotencyKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(value));
        }

        if (value.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Idempotency key cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new IdempotencyKey(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}