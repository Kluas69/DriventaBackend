using FluentValidation;
using Driventa.Application.DTOs.Applications;

namespace Driventa.Application.Validation.Validators;

public class PublicApplicationRequestValidator : AbstractValidator<PublicApplicationRequest>
{
    public PublicApplicationRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TruckCount).GreaterThan(0);
    }
}
