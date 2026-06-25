using Domain.Auth;

namespace Application.Auth;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}