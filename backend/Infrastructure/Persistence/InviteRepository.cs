using Application.Invites;
using Domain.Invites;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class InviteRepository(SolicitationsDbContext context) : IInviteRepository
{
    public void Add(Invite invite) => context.Invites.Add(invite);

    public Task<Invite?> GetByIdAsync(Guid id, CancellationToken ct)
        => context.Invites.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
        => context.Invites.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<Invite>> ListAsync(CancellationToken ct)
        => await context.Invites.AsNoTracking().OrderByDescending(i => i.CreatedAtUtc).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}