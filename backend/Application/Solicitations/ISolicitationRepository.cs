using Domain.Solicitation;

namespace Application.Solicitations;

public interface ISolicitationRepository
{
    Task<IReadOnlyList<Solicitation>> ListAsync(Guid ownerId, CancellationToken ct);
    Task<Solicitation?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken ct);
    void Add(Solicitation solicitation);
    void Remove(Solicitation solicitation);
    Task SaveChangesAsync(CancellationToken ct);
}