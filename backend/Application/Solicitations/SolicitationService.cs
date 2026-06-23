using Application.Common.Exceptions;
using Domain.Solicitation;

namespace Application.Solicitations;

public sealed class SolicitationService(ISolicitationRepository repository)
{
    public async Task<IReadOnlyList<SolicitationResponse>> ListAsync(CancellationToken ct)
    {
        var items = await repository.ListAsync(ct);
        return items.Select(s => s.ToResponse()).ToList();
    }
    
    public async Task<SolicitationResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);
        return solicitation.ToResponse();
    }

    public async Task<SolicitationResponse> CreateAsync(CreateSolicitationRequest request, CancellationToken ct)
    {
        var solicitation = new Solicitation(
            request.JobName,
            request.Location.ToDomain(),
            request.Website.ToDomain(),
            request.Contact.ToDomain());

        repository.Add(solicitation);
        await repository.SaveChangesAsync(ct);
        return solicitation.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateSolicitationRequest request, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);
        
        solicitation.UpdateDetails(request.JobName, request.Location.ToDomain(),
            request.Website.ToDomain(), request.Contact.ToDomain());
        solicitation.ChangeStatus(request.Status);
        
        await repository.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var solicitation = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Solicitation), id);

        repository.Remove(solicitation);
        await repository.SaveChangesAsync(ct);
    }
}