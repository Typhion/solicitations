using Application.Auth;
using Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class RefreshTokenRepository(SolicitationsDbContext context) : IRefreshTokenRepository
{
    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    // Tracked (no AsNoTracking) — the caller mutates the result via Revoke().
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
        => context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
