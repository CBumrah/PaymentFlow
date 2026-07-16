using MediatR;
using PaymentFlow.Domain.ValueObjects;
using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Application.Payments.Commands.CreatePayment;

/// <summary>
/// Request to create a new payment. Carries plain primitive types (not
/// domain types like Money) because Commands are the boundary where raw
/// input from the API gets accepted — the handler is responsible for
/// converting these into validated domain value objects.
/// </summary>
public sealed record CreatePaymentCommand(
    Guid MerchantId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<Result<CreatePaymentResult>>;

/// <summary>
/// What the handler hands back to the caller (eventually the API
/// controller) on success.
/// </summary>
public sealed record CreatePaymentResult(
    Guid PaymentId,
    PaymentStatus Status,
    decimal Amount,
    string Currency);