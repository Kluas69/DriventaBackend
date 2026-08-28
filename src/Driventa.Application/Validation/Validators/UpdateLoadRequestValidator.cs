using FluentValidation;
using Driventa.Application.DTOs.Loads;

namespace Driventa.Application.Validation.Validators;

public class UpdateLoadRequestValidator : AbstractValidator<UpdateLoadRequest>
{
    public UpdateLoadRequestValidator()
    {
        RuleFor(x => x.PickupCity).MaximumLength(100);
        RuleFor(x => x.PickupState).MaximumLength(50);
        RuleFor(x => x.DeliveryCity).MaximumLength(100);
        RuleFor(x => x.DeliveryState).MaximumLength(50);
        RuleFor(x => x.Rate).GreaterThan(0).When(x => x.Rate.HasValue);
        RuleFor(x => x.Miles).GreaterThan(0).When(x => x.Miles.HasValue);
        RuleFor(x => x.DispatchFeeType).MaximumLength(50);
        RuleFor(x => x.DispatchFeeValue).GreaterThanOrEqualTo(0).When(x => x.DispatchFeeValue.HasValue);
        RuleFor(x => x.DeliveryDateTime).GreaterThan(x => x.PickupDateTime)
            .When(x => x.PickupDateTime.HasValue && x.DeliveryDateTime.HasValue);
        RuleFor(x => x).Must(x =>
            x.TruckId.HasValue ||
            x.DriverId.HasValue ||
            x.BrokerId.HasValue ||
            !string.IsNullOrEmpty(x.PickupCity) ||
            !string.IsNullOrEmpty(x.PickupState) ||
            x.PickupDateTime.HasValue ||
            !string.IsNullOrEmpty(x.DeliveryCity) ||
            !string.IsNullOrEmpty(x.DeliveryState) ||
            x.DeliveryDateTime.HasValue ||
            x.Rate.HasValue ||
            x.Miles.HasValue ||
            !string.IsNullOrEmpty(x.DispatchFeeType) ||
            x.DispatchFeeValue.HasValue
        ).WithMessage("At least one field must be provided for update.");
    }
}
