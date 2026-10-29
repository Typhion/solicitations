using FluentValidation;
using WebApi.Api;

namespace WebApi.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class TwoFactorLoginRequestValidator : AbstractValidator<TwoFactorLoginRequest>
{
    public TwoFactorLoginRequestValidator()
    {
        RuleFor(x => x.ChallengeToken).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
    }
}