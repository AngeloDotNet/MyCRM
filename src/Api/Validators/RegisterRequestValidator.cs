using Api.DTOs;
using FluentValidation;

namespace Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("La password deve avere almeno 8 caratteri.");
    }
}