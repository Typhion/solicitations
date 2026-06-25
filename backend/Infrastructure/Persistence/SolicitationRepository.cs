using Application.Solicitations;
using Domain.Solicitation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class SolicitationRepository(SolicitationsDbContext context) : ISolicitationRepository
{
    public async Task<IReadOnlyList<Solicitation>> ListAsync(Guid ownerId, int skip, int take, CancellationToken ct)
        => await context.Solicitations.AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderBy(s => s.Id)            // stable order so paging is deterministic
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountAsync(Guid ownerId, CancellationToken ct)
        => context.Solicitations.CountAsync(s => s.OwnerId == ownerId, ct);

    public Task<Solicitation?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken ct)
        => context.Solicitations.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

    public void Add(Solicitation solicitation) => context.Solicitations.Add(solicitation);

    public void Remove(Solicitation solicitation) => context.Solicitations.Remove(solicitation);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}