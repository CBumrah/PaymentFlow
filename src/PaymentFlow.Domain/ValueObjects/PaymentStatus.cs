namespace PaymentFlow.Domain.ValueObjects;

/// <summary>
/// Represents the lifecycle state of a Payment. Modeled as an enum (rather
/// than a class hierarchy) because the transition rules are simple enough
/// to enforce inside the Payment aggregate itself — see
/// Payment.EnsureValidTransition() in the aggregate root.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment has been created but not yet sent to the provider.</summary>
    Pending = 0,

    /// <summary>Payment has been submitted to the provider and is awaiting confirmation.</summary>
    Processing = 1,

    /// <summary>Provider has confirmed the funds were captured successfully.</summary>
    Captured = 2,

    /// <summary>Provider reported the payment failed (declined, insufficient funds, etc.).</summary>
    Failed = 3,

    /// <summary>A previously captured payment has been fully refunded.</summary>
    Refunded = 4,

    /// <summary>A previously captured payment has been partially refunded.</summary>
    PartiallyRefunded = 5,

    /// <summary>Payment was cancelled before capture (e.g., by merchant or customer).</summary>
    Cancelled = 6
}

/// <summary>
/// Defines which PaymentStatus transitions are legal. Centralizing this
/// keeps the Payment aggregate's Capture()/Refund()/Cancel() methods from
/// duplicating transition logic and gives us one place to unit-test the
/// entire state machine.
/// </summary>
public static class PaymentStatusTransitions
{
    private static readonly Dictionary<PaymentStatus, PaymentStatus[]> AllowedTransitions = new()
    {
        [PaymentStatus.Pending] = new[] { PaymentStatus.Processing, PaymentStatus.Cancelled },
        [PaymentStatus.Processing] = new[] { PaymentStatus.Captured, PaymentStatus.Failed },
        [PaymentStatus.Captured] = new[] { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded },
        [PaymentStatus.PartiallyRefunded] = new[] { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded },
        [PaymentStatus.Failed] = Array.Empty<PaymentStatus>(),
        [PaymentStatus.Refunded] = Array.Empty<PaymentStatus>(),
        [PaymentStatus.Cancelled] = Array.Empty<PaymentStatus>()
    };

    public static bool CanTransition(PaymentStatus from, PaymentStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}