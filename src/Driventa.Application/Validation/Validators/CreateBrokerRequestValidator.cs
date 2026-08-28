using FluentValidation;
using Driventa.Application.DTOs.Brokers;

namespace Driventa.Application.Validation.Validators;

public class CreateBrokerRequestValidator : AbstractValidator<CreateBrokerRequest>
{
    public CreateBrokerRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.McNumber).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.InternalRating).InclusiveBetween(1, 5).When(x => x.InternalRating.HasValue);
        RuleFor(x => x.PaymentNotes).MaximumLength(2000);
        RuleFor(x => x.GeneralNotes).MaximumLength(2000);
    }
}
