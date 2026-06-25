namespace Application.Users;

public interface IUserDirectory
{
    Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct);
    Task<UserSummary?> FindAsync(Guid id, CancellationToken ct);
    Task<int> CountInRoleAsync(string role, CancellationToken ct);
    Task AddToRoleAsync(Guid id, string role, CancellationToken ct);
    Task RemoveFromRoleAsync(Guid id, string role, CancellationToken ct);
    Task SetLockedAsync(Guid id, bool locked, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}