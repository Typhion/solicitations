using Application.Account;
using FluentValidation;

namespace WebApi.Validation;

// Only shape checks here; the password policy itself is enforced by Identity.

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage("The new password must be different from the current one.");
    }
}

public sealed class EnableTwoFactorRequestValidator : AbstractValidator<EnableTwoFactorRequest>
{
    public EnableTwoFactorRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
    }
}

public sealed class PasswordConfirmationRequestValidator : AbstractValidator<PasswordConfirmationRequest>
{
    public PasswordConfirmationRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
    }
}
