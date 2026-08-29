using Driventa.Application.DTOs.Roles;
using FluentValidation;

namespace Driventa.Application.Validation.Validators;

public class UpdateRolePermissionsRequestValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("Permission IDs are required.");

        RuleForEach(x => x.PermissionIds)
            .NotEmpty().WithMessage("Permission ID cannot be empty.")
            .Must(id => id != Guid.Empty).WithMessage("Permission ID must be a valid GUID.");
    }
}
