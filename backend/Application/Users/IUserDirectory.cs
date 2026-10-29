namespace Application.Users;

public interface IUserDirectory
{
    Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct);
    Task<UserSummary?> FindAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
    Task<int> CountInRoleAsync(string role, CancellationToken ct);
    Task AddToRoleAsync(Guid id, string role, CancellationToken ct);
    Task RemoveFromRoleAsync(Guid id, string role, CancellationToken ct);

    /// <summary>Locking also revokes the user's refresh tokens (signed out within one access-token lifetime).</summary>
    Task SetLockedAsync(Guid id, bool locked, CancellationToken ct);

    /// <summary>Deletes the user together with their solicitations (+ meetings) and refresh tokens, atomically.</summary>
    Task DeleteWithDataAsync(Guid id, CancellationToken ct);
}