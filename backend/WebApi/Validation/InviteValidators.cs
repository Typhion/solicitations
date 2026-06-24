using Application.Common;
using Application.Invites;
using FluentValidation;

namespace WebApi.Validation;

public sealed class CreateInviteRequestValidator : AbstractValidator<CreateInviteRequest>
{
    public CreateInviteRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(Roles.Assignable.Contains)
            .WithMessage("Role must be one of: " + string.Join(", ", Roles.Assignable));

        When(x => x.Email is not null, () =>
            RuleFor(x => x.Email!).EmailAddress().MaximumLength(200));

        When(x => x.ExpiresInDays is not null, () =>
            RuleFor(x => x.ExpiresInDays!.Value).InclusiveBetween(1, 90));
    }
}