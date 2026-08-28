using FluentValidation;
using Driventa.Application.DTOs.Brokers;

namespace Driventa.Application.Validation.Validators;

public class UpdateBrokerRequestValidator : AbstractValidator<UpdateBrokerRequest>
{
    public UpdateBrokerRequestValidator()
    {
        RuleFor(x => x.CompanyName).MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.McNumber).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.InternalRating).InclusiveBetween(1, 5).When(x => x.InternalRating.HasValue);
        RuleFor(x => x.PaymentNotes).MaximumLength(2000);
        RuleFor(x => x.GeneralNotes).MaximumLength(2000);
        RuleFor(x => x).Must(x =>
            !string.IsNullOrEmpty(x.CompanyName) ||
            !string.IsNullOrEmpty(x.ContactName) ||
            !string.IsNullOrEmpty(x.Email) ||
            !string.IsNullOrEmpty(x.Phone) ||
            !string.IsNullOrEmpty(x.McNumber) ||
            !string.IsNullOrEmpty(x.Address) ||
            x.InternalRating.HasValue ||
            !string.IsNullOrEmpty(x.PaymentNotes) ||
            !string.IsNullOrEmpty(x.GeneralNotes) ||
            x.IsActive.HasValue
        ).WithMessage("At least one field must be provided for update.");
    }
}
