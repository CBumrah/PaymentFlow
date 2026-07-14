using PaymentFlow.Domain.Events;
using PaymentFlow.Domain.Exceptions;
using PaymentFlow.Domain.ValueObjects;
using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Domain.Entities;

/// <summary>
/// The Payment aggregate root — the consistency boundary for a single
/// payment's lifecycle. All state changes happen through behavior methods
/// (Capture, Fail, Cancel, Refund) rather than public setters, and every
/// transition is checked against PaymentStatusTransitions before it's
/// allowed to happen. This is what makes "Payment.Status = X" impossible
/// from outside the aggregate — callers can only ask the payment to do
/// something, never force it into an arbitrary state.
/// </summary>
public sealed class Payment : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public Money RefundedAmount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public IdempotencyKey IdempotencyKey { get; private set; } = null!;
    public string? ProviderName { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Required by EF Core.
    private Payment()
    {
    }

    private Payment(Guid id, Guid merchantId, Money amount, IdempotencyKey idempotencyKey, DateTime nowUtc)
        : base(id)
    {
        MerchantId = merchantId;
        Amount = amount;
        RefundedAmount = Money.Zero(amount.Currency);
        Status = PaymentStatus.Pending;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Creates a new Payment in the Pending state and raises PaymentCreatedEvent.
    /// This is the only way to construct a Payment — there is no public
    /// constructor, which forces every payment through validated creation logic.
    /// </summary>
    public static Payment Create(Guid merchantId, Money amount, IdempotencyKey idempotencyKey, DateTime nowUtc)
    {
        if (merchantId == Guid.Empty)
        {
            throw new ArgumentException("MerchantId cannot be empty.", nameof(merchantId));
        }

        if (amount.AmountInMinorUnits <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));
        }

        var payment = new Payment(Guid.NewGuid(), merchantId, amount, idempotencyKey, nowUtc);

        payment.RaiseDomainEvent(new PaymentCreatedEvent(
            payment.Id, payment.MerchantId, payment.Amount, nowUtc));

        return payment;
    }

    /// <summary>
    /// Marks the payment as submitted to a provider and awaiting confirmation.
    /// </summary>
    public void BeginProcessing(string providerName, DateTime nowUtc)
    {
        EnsureValidTransition(PaymentStatus.Processing);

        Status = PaymentStatus.Processing;
        ProviderName = providerName;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Marks the payment as successfully captured by the provider.
    /// </summary>
    public void Capture(string providerReference, DateTime nowUtc)
    {
        EnsureValidTransition(PaymentStatus.Captured);

        Status = PaymentStatus.Captured;
        ProviderReference = providerReference;
        UpdatedAtUtc = nowUtc;

        RaiseDomainEvent(new PaymentCapturedEvent(Id, MerchantId, Amount, nowUtc));
    }

    /// <summary>
    /// Marks the payment as failed — either declined by the provider or
    /// failed during processing.
    /// </summary>
    public void Fail(string reason, DateTime nowUtc)
    {
        EnsureValidTransition(PaymentStatus.Failed);

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        UpdatedAtUtc = nowUtc;

        RaiseDomainEvent(new PaymentFailedEvent(Id, MerchantId, reason, nowUtc));
    }

    /// <summary>
    /// Cancels a payment before it has been captured (e.g., merchant or
    /// customer cancels before funds are taken).
    /// </summary>
    public void Cancel(DateTime nowUtc)
    {
        EnsureValidTransition(PaymentStatus.Cancelled);

        Status = PaymentStatus.Cancelled;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Refunds all or part of a captured payment. Refunding the full
    /// remaining captured amount transitions to Refunded; refunding less
    /// than that transitions to PartiallyRefunded.
    /// </summary>
    public void Refund(Money refundAmount, DateTime nowUtc)
    {
        var newTotalRefunded = RefundedAmount.Add(refundAmount);
        var isFullRefund = newTotalRefunded.IsGreaterThanOrEqualTo(Amount);

        var targetStatus = isFullRefund ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        EnsureValidTransition(targetStatus);

        if (newTotalRefunded.IsGreaterThan(Amount))
        {
            throw new InvalidOperationException("Refund amount exceeds the captured payment amount.");
        }

        RefundedAmount = newTotalRefunded;
        Status = targetStatus;
        UpdatedAtUtc = nowUtc;

        RaiseDomainEvent(new PaymentRefundedEvent(Id, MerchantId, refundAmount, isFullRefund, nowUtc));
    }

    private void EnsureValidTransition(PaymentStatus targetStatus)
    {
        // PartiallyRefunded -> PartiallyRefunded is allowed (multiple partial
        // refunds), so it's excluded from the strict "must actually change" check.
        var isRepeatedPartialRefund =
            Status == PaymentStatus.PartiallyRefunded && targetStatus == PaymentStatus.PartiallyRefunded;

        if (!isRepeatedPartialRefund && !PaymentStatusTransitions.CanTransition(Status, targetStatus))
        {
            throw new InvalidPaymentStateTransitionException(Status, targetStatus);
        }
    }
}