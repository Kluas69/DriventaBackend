using FluentValidation;
using Driventa.Application.DTOs.Auth;

namespace Driventa.Application.Validation.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(500);
    }
}
