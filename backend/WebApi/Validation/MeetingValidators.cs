using Application.Solicitations;
using FluentValidation;

namespace WebApi.Validation;

public sealed class AddMeetingRequestValidator : AbstractValidator<AddMeetingRequest>
{
    public AddMeetingRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        When(x => x.IsOnline, () =>
            RuleFor(x => x.OnlineTool).NotEmpty().MaximumLength(100));
    }
}
