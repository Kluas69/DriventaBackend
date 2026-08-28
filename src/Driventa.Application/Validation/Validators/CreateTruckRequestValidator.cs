using FluentValidation;
using Driventa.Application.DTOs.Trucks;

namespace Driventa.Application.Validation.Validators;

public class CreateTruckRequestValidator : AbstractValidator<CreateTruckRequest>
{
    public CreateTruckRequestValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.TruckNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, 2100).When(x => x.Year.HasValue);
        RuleFor(x => x.LicensePlate).MaximumLength(20);
        RuleFor(x => x.LicenseState).MaximumLength(50);
    }
}
