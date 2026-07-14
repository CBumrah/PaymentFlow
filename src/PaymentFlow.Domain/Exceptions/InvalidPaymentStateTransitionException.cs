using PaymentFlow.Domain.ValueObjects;

namespace PaymentFlow.Domain.Exceptions;

/// <summary>
/// Thrown when code attempts an illegal PaymentStatus transition, e.g.
/// capturing an already-refunded payment. Kept as a distinct, named
/// exception (rather than a generic InvalidOperationException) so callers
/// can catch it specifically and translate it into a clean API error
/// response instead of a 500.
/// </summary>
public sealed class InvalidPaymentStateTransitionException : DomainException
{
    public PaymentStatus FromStatus { get; }
    public PaymentStatus ToStatus { get; }

    public InvalidPaymentStateTransitionException(PaymentStatus fromStatus, PaymentStatus toStatus)
        : base($"Cannot transition payment from '{fromStatus}' to '{toStatus}'.")
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}