using FluentValidation;
using Driventa.Application.DTOs.Loads;

namespace Driventa.Application.Validation.Validators;

public class CreateLoadRequestValidator : AbstractValidator<CreateLoadRequest>
{
    public CreateLoadRequestValidator()
    {
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.PickupCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PickupState).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PickupDateTime).NotEmpty();
        RuleFor(x => x.DeliveryCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeliveryState).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DeliveryDateTime).NotEmpty();
        RuleFor(x => x.Rate).GreaterThan(0);
        RuleFor(x => x.Miles).GreaterThan(0).When(x => x.Miles.HasValue);
        RuleFor(x => x.DispatchFeeType).MaximumLength(50);
        RuleFor(x => x.DispatchFeeValue).GreaterThanOrEqualTo(0).When(x => x.DispatchFeeValue.HasValue);
        RuleFor(x => x.DeliveryDateTime).GreaterThan(x => x.PickupDateTime);
    }
}
