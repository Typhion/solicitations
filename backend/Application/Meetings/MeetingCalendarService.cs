using Application.Common;
using Application.Solicitations;

namespace Application.Meetings;

public sealed class MeetingCalendarService(ISolicitationRepository repository, ICurrentUser currentUser)
{
    // Not paginated: the date range (capped by the validator) is the bound.
    public Task<IReadOnlyList<CalendarMeetingDto>> ListAsync(MeetingCalendarQuery query, CancellationToken ct)
        => repository.ListMeetingsAsync(currentUser.Id, query.From.UtcDateTime, query.To.UtcDateTime, query.SolicitationId, ct);
}
