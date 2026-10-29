using Application.Meetings;
using FluentValidation;

namespace WebApi.Validation;

public sealed class MeetingCalendarQueryValidator : AbstractValidator<MeetingCalendarQuery>
{
    // Enough for a 6-week month grid with room to spare; keeps the unpaginated list bounded.
    public static readonly TimeSpan MaxRange = TimeSpan.FromDays(100);

    public MeetingCalendarQueryValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage("'To' must be after 'From'.");
        RuleFor(x => x).Must(x => x.To - x.From <= MaxRange)
            .WithName("To")
            .WithMessage($"The range may span at most {MaxRange.TotalDays} days.");
    }
}
