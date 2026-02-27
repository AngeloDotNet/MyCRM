using Api.DTOs;
using FluentValidation;

namespace Api.Validators;

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}