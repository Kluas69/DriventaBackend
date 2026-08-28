using FluentValidation;
using Driventa.Application.DTOs.Drivers;

namespace Driventa.Application.Validation.Validators;

public class CreateDriverRequestValidator : AbstractValidator<CreateDriverRequest>
{
    public CreateDriverRequestValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.LicenseNumber).MaximumLength(50);
        RuleFor(x => x.LicenseState).MaximumLength(50);
    }
}
