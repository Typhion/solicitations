using Application.Auth;
using FluentValidation;

namespace WebApi.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty();
    }
}
