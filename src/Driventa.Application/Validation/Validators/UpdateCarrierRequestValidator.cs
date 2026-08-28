using FluentValidation;
using Driventa.Application.DTOs.Carriers;

namespace Driventa.Application.Validation.Validators;

public class UpdateCarrierRequestValidator : AbstractValidator<UpdateCarrierRequest>
{
    public UpdateCarrierRequestValidator()
    {
        RuleFor(x => x.CompanyName).MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.McNumber).MaximumLength(50);
        RuleFor(x => x.DotNumber).MaximumLength(50);
        RuleFor(x => x.AddressLine1).MaximumLength(300);
        RuleFor(x => x.AddressLine2).MaximumLength(300);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(50);
        RuleFor(x => x.ZipCode).MaximumLength(20);
        RuleFor(x => x.PreferredLanes).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x).Must(x =>
            !string.IsNullOrEmpty(x.CompanyName) ||
            !string.IsNullOrEmpty(x.ContactName) ||
            !string.IsNullOrEmpty(x.Email) ||
            !string.IsNullOrEmpty(x.Phone) ||
            !string.IsNullOrEmpty(x.McNumber) ||
            !string.IsNullOrEmpty(x.DotNumber) ||
            !string.IsNullOrEmpty(x.AddressLine1) ||
            !string.IsNullOrEmpty(x.AddressLine2) ||
            !string.IsNullOrEmpty(x.City) ||
            !string.IsNullOrEmpty(x.State) ||
            !string.IsNullOrEmpty(x.ZipCode) ||
            x.Status.HasValue ||
            !string.IsNullOrEmpty(x.PreferredLanes) ||
            !string.IsNullOrEmpty(x.Notes)
        ).WithMessage("At least one field must be provided for update.");
    }
}
