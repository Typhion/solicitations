using Domain.Meeting;
using Domain.Solicitation;

namespace Application.Solicitations;

public sealed record CreateSolicitationRequest(string JobName, LocationDto Location, WebsiteDto Website, ContactDto Contact);

public sealed record UpdateSolicitationRequest(string JobName, LocationDto Location, WebsiteDto Website, ContactDto Contact, SolicitationStatus Status);

public sealed record SolicitationResponse(Guid Id, string JobName, SolicitationStatus Status, LocationDto Location, WebsiteDto Website, ContactDto Contact, IReadOnlyList<MeetingDto> Meetings);

public sealed record AddMeetingRequest(DateTime ScheduledAtUtc, DateTime EndsAtUtc, MeetingType Type, bool IsOnline, string? OnlineTool);

public sealed record UpdateMeetingRequest(DateTime ScheduledAtUtc, DateTime EndsAtUtc, MeetingType Type, bool IsOnline, string? OnlineTool);

public sealed record MeetingDto(Guid Id, DateTime ScheduledAtUtc, DateTime EndsAtUtc, MeetingType Type, bool IsOnline, string? OnlineTool);

/// <summary>
/// Request: <see cref="Validated"/> = "verify this address with Google" (EU only; off = free text, not on
/// the map). <see cref="PlaceId"/> is the picked suggestion, if any; coordinates sent are ignored.
/// Response: <see cref="Validated"/> = "this address is verified"; place id and coordinates are set then.
/// </summary>
public sealed record LocationDto(
    string Country, string City, string ZipCode, string Street, string StreetNumber,
    bool Validated = false, string? PlaceId = null, double? Latitude = null, double? Longitude = null);

public sealed record WebsiteDto(string Name, string Link);
public sealed record ContactDto(string Name, string PhoneNumber, string Email);