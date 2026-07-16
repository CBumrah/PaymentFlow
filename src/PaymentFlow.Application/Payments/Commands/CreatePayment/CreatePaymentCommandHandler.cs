using MediatR;
using PaymentFlow.Application.Common.Interfaces;
using PaymentFlow.Domain.Entities;
using PaymentFlow.Domain.ValueObjects;
using PaymentFlow.SharedKernel.Errors;
using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Application.Payments.Commands.CreatePayment;

/// <summary>
/// Handles CreatePaymentCommand. Enforces idempotency (returning the
/// existing payment if this key was already used) before ever touching
/// the Payment aggregate, and returns Result; rather than throwing
/// for expected business outcomes like duplicate requests.
/// </summary>
public sealed class CreatePaymentCommandHandler
    : IRequestHandler<CreatePaymentCommand, Result<CreatePaymentResult>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly TimeProvider _timeProvider;

    public CreatePaymentCommandHandler(IPaymentRepository paymentRepository, TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreatePaymentResult>> Handle(
        CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var idempotencyKey = IdempotencyKey.Create(request.IdempotencyKey);

        var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingPayment is not null)
        {
            // Not an error — the client retried a request it already made.
            // Returning the original result keeps the operation idempotent.
            return Result.Success(ToResult(existingPayment));
        }

        Money amount;
        try
        {
            amount = Money.FromDecimal(request.Amount, request.Currency);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CreatePaymentResult>(
                Error.Validation("Payment.InvalidAmount", ex.Message));
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var payment = Payment.Create(request.MerchantId, amount, idempotencyKey, nowUtc);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResult(payment));
    }

    private static CreatePaymentResult ToResult(Payment payment) => new(
        payment.Id,
        payment.Status,
        payment.Amount.AsDecimal(),
        payment.Amount.Currency);
}