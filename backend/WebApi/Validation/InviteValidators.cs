using Application.Invites;
using FluentValidation;

namespace WebApi.Validation;

public sealed class CreateInviteRequestValidator : AbstractValidator<CreateInviteRequest>
{
    public CreateInviteRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);

        When(x => x.ExpiresInDays is not null, () =>
            RuleFor(x => x.ExpiresInDays!.Value).InclusiveBetween(1, 90));
    }
}
