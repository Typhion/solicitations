using Domain.Solicitation;

namespace Application.Solicitations;

internal static class SolicitationMappings
{
    public static SolicitationResponse ToResponse(this Solicitation s) => new(
        s.Id, s.JobName, s.Status,
        new LocationDto(s.Location.Country, s.Location.City, s.Location.ZipCode, s.Location.Street, s.Location.StreetNumber,
            s.Location.IsVerified, s.Location.PlaceId, s.Location.Latitude, s.Location.Longitude),
        new WebsiteDto(s.Website.Name, s.Website.Link),
        new ContactDto(s.Contact.Name, s.Contact.PhoneNumber, s.Contact.Email),
        s.Meetings.Select(m => new MeetingDto(m.Id, m.ScheduledAtUtc, m.Type, m.IsOnline, m.OnlineTool)).ToList());

    public static Website ToDomain(this WebsiteDto d) => new(d.Name, d.Link);
    public static Contact ToDomain(this ContactDto d) => new(d.Name, d.PhoneNumber, d.Email);
}