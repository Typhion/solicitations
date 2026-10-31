using Application.Account;
using Application.Common;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace WebApi.Validation;

// Only shape checks here; the password policy itself is enforced by Identity.

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128)
            .NotEqual(x => x.CurrentPassword).WithMessage(_ => localizer["The new password must be different from the current one."]);
    }
}

public sealed class SetLanguageRequestValidator : AbstractValidator<SetLanguageRequest>
{
    public SetLanguageRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Language).NotEmpty().Must(l => Languages.All.Contains(l))
            .WithMessage(_ => localizer["Language must be one of: {0}.", string.Join(", ", Languages.All)]);
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
