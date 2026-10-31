using Application.Common;
using Application.Common.Exceptions;
using Application.Places;
using Domain.Core;
using Domain.Solicitation;

namespace Application.Solicitations;

public sealed class SolicitationService(
    ISolicitationRepository repository, ICurrentUser currentUser, IPlacesClient places, TimeProvider clock)
{
    public async Task<PagedResult<SolicitationResponse>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await repository.CountAsync(currentUser.Id, ct);
        var items = await repository.ListAsync(currentUser.Id, (page - 1) * pageSize, pageSize, ct);
        return new PagedResult<SolicitationResponse>(items.Select(s => s.ToResponse()).ToList(), page, pageSize, total);
    }
    
    public async Task<SolicitationResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var s = await repository.GetByIdAsync(id, currentUser.Id, ct)
                ?? throw new NotFoundException(nameof(Solicitation), id);
        return s.ToResponse();
    }

    public async Task<SolicitationResponse> CreateAsync(CreateSolicitationRequest request, CancellationToken ct)
    {
        var location = await ResolveLocationAsync(request.Location, current: null, ct);
        var s = new Solicitation(currentUser.Id, request.JobName, location,
            request.Website.ToDomain(), request.Contact.ToDomain());
        repository.Add(s);
        await repository.SaveChangesAsync(ct);
        return s.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateSolicitationRequest request, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);
        
        var location = await ResolveLocationAsync(request.Location, solicitation.Location, ct);
        solicitation.UpdateDetails(request.JobName, location,
            request.Website.ToDomain(), request.Contact.ToDomain());
        solicitation.ChangeStatus(request.Status);
        
        await repository.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);

        repository.Remove(solicitation);
        await repository.SaveChangesAsync(ct);
    }

    public async Task<SolicitationResponse> AddMeetingAsync(Guid id, AddMeetingRequest req, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);

        solicitation.AddMeeting(req.ScheduledAtUtc, req.EndsAtUtc, req.Type, req.IsOnline, req.OnlineTool);
        await repository.SaveChangesAsync(ct);
        return solicitation.ToResponse();
    }

    public async Task UpdateMeetingAsync(Guid id, Guid meetingId, UpdateMeetingRequest req, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);

        if (solicitation.Meetings.All(m => m.Id != meetingId))
            throw new NotFoundException("Meeting", meetingId);

        solicitation.UpdateMeeting(meetingId, req.ScheduledAtUtc, req.EndsAtUtc, req.Type, req.IsOnline, req.OnlineTool);
        await repository.SaveChangesAsync(ct);
    }

    public async Task RemoveMeetingAsync(Guid id, Guid meetingId, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);

        if (solicitation.Meetings.All(m => m.Id != meetingId))
            throw new NotFoundException("Meeting", meetingId);

        solicitation.RemoveMeeting(meetingId);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Free text when verification is off. Otherwise Google must resolve the address: the picked place
    /// (<see cref="LocationDto.PlaceId"/>) or, when none was picked, the best match for the typed fields.
    /// The stored fields are Google's. An unchanged verified address is kept as-is, so editing other
    /// fields doesn't spend a lookup.
    /// </summary>
    private async Task<Location> ResolveLocationAsync(LocationDto dto, Location? current, CancellationToken ct)
    {
        if (!dto.Validated)
            return Location.Unverified(dto.Country, dto.City, dto.ZipCode, dto.Street, dto.StreetNumber);

        if (current is { IsVerified: true }
            && (dto.PlaceId is null || dto.PlaceId == current.PlaceId)
            && current.HasSameAddressAs(dto.Country, dto.City, dto.ZipCode, dto.Street, dto.StreetNumber))
            return current;

        PlaceAddress? place;
        try
        {
            place = dto.PlaceId is not null
                ? await places.GetPlaceAsync(dto.PlaceId, sessionToken: null, ct)
                : await places.FindAddressAsync(
                    new AddressQuery(dto.Country, dto.City, dto.ZipCode, dto.Street, dto.StreetNumber), ct);
        }
        catch (PlacesUnavailableException)
        {
            throw new DomainException(
                "Address verification is unavailable right now. Try again later, or save the address as free text.");
        }

        if (place is null)
            throw new DomainException(
                "This address could not be verified. Pick a suggestion from the search, or save it as free text.");

        if (!EuCountries.Contains(place.CountryCode))
            throw new DomainException(
                "Only addresses in the EU can be verified. Save this one as free text instead.");

        // A company listed without a house number keeps the one that was typed.
        var number = string.IsNullOrWhiteSpace(place.StreetNumber) ? dto.StreetNumber : place.StreetNumber;
        return Location.Verified(place.Country, place.City, place.ZipCode, place.Street, number,
            place.PlaceId, place.Latitude, place.Longitude, clock.GetUtcNow().UtcDateTime);
    }
}
