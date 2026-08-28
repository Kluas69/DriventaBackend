using FluentValidation;
using Driventa.Application.DTOs.Invoices;

namespace Driventa.Application.Validation.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).MaximumLength(50);
        RuleFor(x => x.TransactionReference).MaximumLength(200);
    }
}
