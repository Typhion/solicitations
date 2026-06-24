using Domain.Invites;

namespace Application.Invites;

public interface IInviteRepository
{
    void Add(Invite invite);
    Task<Invite?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Invite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<Invite>> ListAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}