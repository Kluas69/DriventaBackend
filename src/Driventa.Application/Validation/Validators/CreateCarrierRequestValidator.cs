using FluentValidation;
using Driventa.Application.DTOs.Carriers;

namespace Driventa.Application.Validation.Validators;

public class CreateCarrierRequestValidator : AbstractValidator<CreateCarrierRequest>
{
    public CreateCarrierRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.McNumber).MaximumLength(50);
        RuleFor(x => x.DotNumber).MaximumLength(50);
        RuleFor(x => x.AddressLine1).MaximumLength(300);
        RuleFor(x => x.AddressLine2).MaximumLength(300);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(50);
        RuleFor(x => x.ZipCode).MaximumLength(20);
        RuleFor(x => x.PreferredLanes).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
