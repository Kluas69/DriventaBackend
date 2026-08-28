using FluentValidation;
using Driventa.Application.DTOs.Drivers;

namespace Driventa.Application.Validation.Validators;

public class UpdateDriverRequestValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.LicenseNumber).MaximumLength(50);
        RuleFor(x => x.LicenseState).MaximumLength(50);
        RuleFor(x => x).Must(x =>
            x.TruckId.HasValue ||
            !string.IsNullOrEmpty(x.FirstName) ||
            !string.IsNullOrEmpty(x.LastName) ||
            !string.IsNullOrEmpty(x.Email) ||
            !string.IsNullOrEmpty(x.Phone) ||
            !string.IsNullOrEmpty(x.LicenseNumber) ||
            !string.IsNullOrEmpty(x.LicenseState) ||
            x.Status.HasValue
        ).WithMessage("At least one field must be provided for update.");
    }
}
