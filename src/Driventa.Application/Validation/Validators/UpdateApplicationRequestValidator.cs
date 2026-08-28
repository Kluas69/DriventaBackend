using FluentValidation;
using Driventa.Application.DTOs.Applications;

namespace Driventa.Application.Validation.Validators;

public class UpdateApplicationRequestValidator : AbstractValidator<UpdateApplicationRequest>
{
    public UpdateApplicationRequestValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.CompanyName).MaximumLength(200);
        RuleFor(x => x.TruckCount).GreaterThan(0).When(x => x.TruckCount.HasValue);
        RuleFor(x => x).Must(x =>
            !string.IsNullOrEmpty(x.FullName) ||
            !string.IsNullOrEmpty(x.Email) ||
            !string.IsNullOrEmpty(x.Phone) ||
            !string.IsNullOrEmpty(x.CompanyName) ||
            x.EquipmentType.HasValue ||
            x.TruckCount.HasValue ||
            !string.IsNullOrEmpty(x.McNumber) ||
            !string.IsNullOrEmpty(x.DotNumber) ||
            !string.IsNullOrEmpty(x.PreferredLanes) ||
            !string.IsNullOrEmpty(x.AdditionalDetails) ||
            x.Status.HasValue
        ).WithMessage("At least one field must be provided for update.");
    }
}
