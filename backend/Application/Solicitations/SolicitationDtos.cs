using Domain.Meeting;
using Domain.Solicitation;

namespace Application.Solicitations;

public sealed record CreateSolicitationRequest(string JobName, LocationDto Location, WebsiteDto Website, ContactDto Contact);

public sealed record UpdateSolicitationRequest(string JobName, LocationDto Location, WebsiteDto Website, ContactDto Contact, SolicitationStatus Status);

public sealed record SolicitationResponse(Guid Id, string JobName, SolicitationStatus Status, LocationDto Location, WebsiteDto Website, ContactDto Contact, IReadOnlyList<MeetingDto> Meetings);

public sealed record AddMeetingRequest(DateTime ScheduledAtUtc, MeetingType Type, bool IsOnline, string? OnlineTool);

public sealed record MeetingDto(Guid Id, DateTime ScheduledAtUtc, MeetingType Type, bool IsOnline, string? OnlineTool);

public sealed record LocationDto(string Country, string City, string ZipCode, string Street, string StreetNumber);
public sealed record WebsiteDto(string Name, string Link);
public sealed record ContactDto(string Name, string PhoneNumber, string Email);