using Application.Meetings;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace WebApi.Validation;

public sealed class MeetingCalendarQueryValidator : AbstractValidator<MeetingCalendarQuery>
{
    // Enough for a 6-week month grid with room to spare; keeps the unpaginated list bounded.
    public static readonly TimeSpan MaxRange = TimeSpan.FromDays(100);

    public MeetingCalendarQueryValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage(_ => localizer["'To' must be after 'From'."]);
        RuleFor(x => x).Must(x => x.To - x.From <= MaxRange)
            .WithName("To")
            .WithMessage(_ => localizer["The range may span at most {0} days.", MaxRange.TotalDays]);
    }
}
