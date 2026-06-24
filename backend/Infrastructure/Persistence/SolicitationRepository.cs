using Application.Solicitations;
using Domain.Solicitation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class SolicitationRepository(SolicitationsDbContext context) : ISolicitationRepository
{
    public async Task<IReadOnlyList<Solicitation>> ListAsync(CancellationToken ct)
        => await context.Solicitations.AsNoTracking().ToListAsync(ct);

    public Task<Solicitation?> GetByIdAsync(Guid id, CancellationToken ct)
        => context.Solicitations.FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Add(Solicitation solicitation) => context.Solicitations.Add(solicitation);

    public void Remove(Solicitation solicitation) => context.Solicitations.Remove(solicitation);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}