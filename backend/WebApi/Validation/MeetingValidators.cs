using Application.Solicitations;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace WebApi.Validation;

public sealed class AddMeetingRequestValidator : AbstractValidator<AddMeetingRequest>
{
    public AddMeetingRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.ScheduledAtUtc)
            .WithMessage(_ => localizer["End time must be after the start time."]);
        When(x => x.IsOnline, () =>
            RuleFor(x => x.OnlineTool).NotEmpty().MaximumLength(100));
    }
}

public sealed class UpdateMeetingRequestValidator : AbstractValidator<UpdateMeetingRequest>
{
    public UpdateMeetingRequestValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.ScheduledAtUtc)
            .WithMessage(_ => localizer["End time must be after the start time."]);
        When(x => x.IsOnline, () =>
            RuleFor(x => x.OnlineTool).NotEmpty().MaximumLength(100));
    }
}
