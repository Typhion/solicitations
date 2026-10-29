using Domain.Invites;

namespace Application.Invites;

public interface IInviteRepository
{
    void Add(Invite invite);
    Task<Invite?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Invite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<Invite>> ListAsync(CancellationToken ct);

    /// <summary>A pending, unexpired invite for this email (case-insensitive) already exists.</summary>
    Task<bool> HasOpenInviteForEmailAsync(string email, DateTime utcNow, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}