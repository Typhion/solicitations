using Domain.Meeting;
using Domain.Solicitation;

namespace Application.Meetings;

/// <summary>Visible calendar range [From, To) plus an optional solicitation filter. Bound from the query string.</summary>
public sealed record MeetingCalendarQuery(DateTimeOffset From, DateTimeOffset To, Guid? SolicitationId);

/// <summary>A meeting flattened with the bits of its solicitation the calendar shows.</summary>
public sealed record CalendarMeetingDto(
    Guid Id,
    Guid SolicitationId,
    string JobName,
    string Company,
    SolicitationStatus Status,
    DateTime ScheduledAtUtc,
    DateTime EndsAtUtc,
    MeetingType Type,
    bool IsOnline,
    string? OnlineTool,
    string City);
