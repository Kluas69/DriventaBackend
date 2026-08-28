using FluentValidation;
using Driventa.Application.DTOs.Trucks;

namespace Driventa.Application.Validation.Validators;

public class UpdateTruckRequestValidator : AbstractValidator<UpdateTruckRequest>
{
    public UpdateTruckRequestValidator()
    {
        RuleFor(x => x.TruckNumber).MaximumLength(50);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, 2100).When(x => x.Year.HasValue);
        RuleFor(x => x.LicensePlate).MaximumLength(20);
        RuleFor(x => x.LicenseState).MaximumLength(50);
        RuleFor(x => x).Must(x =>
            !string.IsNullOrEmpty(x.TruckNumber) ||
            x.EquipmentType.HasValue ||
            !string.IsNullOrEmpty(x.Make) ||
            !string.IsNullOrEmpty(x.Model) ||
            x.Year.HasValue ||
            !string.IsNullOrEmpty(x.LicensePlate) ||
            !string.IsNullOrEmpty(x.LicenseState) ||
            x.Status.HasValue
        ).WithMessage("At least one field must be provided for update.");
    }
}
