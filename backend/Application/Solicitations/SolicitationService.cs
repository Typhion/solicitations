using Application.Common;
using Application.Common.Exceptions;
using Domain.Solicitation;

namespace Application.Solicitations;

public sealed class SolicitationService(ISolicitationRepository repository, ICurrentUser currentUser)
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
        var s = new Solicitation(currentUser.Id, request.JobName, request.Location.ToDomain(),
            request.Website.ToDomain(), request.Contact.ToDomain());
        repository.Add(s);
        await repository.SaveChangesAsync(ct);
        return s.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateSolicitationRequest request, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);
        
        solicitation.UpdateDetails(request.JobName, request.Location.ToDomain(),
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

        solicitation.AddMeeting(req.ScheduledAtUtc, req.Type, req.IsOnline, req.OnlineTool);
        await repository.SaveChangesAsync(ct);
        return solicitation.ToResponse();
    }

    public async Task RemoveMeetingAsync(Guid id, Guid meetingId, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, currentUser.Id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);

        if (solicitation.Meetings.All(m => m.Id != meetingId))
            throw new NotFoundException("Meeting", meetingId);

        solicitation.RemoveMeeting(meetingId);
        await repository.SaveChangesAsync(ct);
    }
}