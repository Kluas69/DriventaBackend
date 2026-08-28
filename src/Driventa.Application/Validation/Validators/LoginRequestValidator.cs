using FluentValidation;
using Driventa.Application.DTOs.Auth;

namespace Driventa.Application.Validation.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}
