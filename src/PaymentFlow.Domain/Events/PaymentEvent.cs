using PaymentFlow.Domain.ValueObjects;
using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Domain.Events;

/// <summary>
/// Raised when a new Payment is created in the Pending state. Infrastructure
/// listens for this (indirectly, via the outbox) to eventually submit the
/// payment to the selected provider.
/// </summary>
public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    Guid MerchantId,
    Money Amount,
    DateTime OccurredAtUtc) : IDomainEvent;

/// <summary>
/// Raised when a provider confirms funds were successfully captured.
/// </summary>
public sealed record PaymentCapturedEvent(
    Guid PaymentId,
    Guid MerchantId,
    Money Amount,
    DateTime OccurredAtUtc) : IDomainEvent;

/// <summary>
/// Raised when a payment fails — either rejected by the provider or failed
/// validation during processing. Carries a reason for downstream logging
/// and merchant-facing error reporting.
/// </summary>
public sealed record PaymentFailedEvent(
    Guid PaymentId,
    Guid MerchantId,
    string Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

/// <summary>
/// Raised when a captured payment is refunded, in full or in part.
/// </summary>
public sealed record PaymentRefundedEvent(
    Guid PaymentId,
    Guid MerchantId,
    Money RefundedAmount,
    bool IsFullRefund,
    DateTime OccurredAtUtc) : IDomainEvent;