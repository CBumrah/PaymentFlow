using FluentValidation;

namespace PaymentFlow.Application.Payments.Commands.CreatePayment;

/// <summary>
/// Validates CreatePaymentCommand shape/format before it ever reaches the
/// handler — this is deliberately shallow validation (required fields,
/// basic format) as opposed to business rule validation (e.g., "merchant
/// must be active"), which belongs in the domain/handler, not here.
/// </summary>
public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("MerchantId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g., USD).");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128)
            .WithMessage("IdempotencyKey is required and cannot exceed 128 characters.");
    }
}