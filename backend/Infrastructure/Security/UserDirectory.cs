using Application.Common.Exceptions;
using Application.Users;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

internal sealed class UserDirectory(UserManager<AppUser> userManager) : IUserDirectory
{
    public async Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct)
    {
        var users = await userManager.Users.ToListAsync(ct);
        var summaries = new List<UserSummary>(users.Count);
        foreach (var user in users)
            summaries.Add(await ToSummaryAsync(user));
        return summaries;
    }

    public async Task<UserSummary?> FindAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : await ToSummaryAsync(user);
    }

    public async Task<int> CountInRoleAsync(string role, CancellationToken ct)
        => (await userManager.GetUsersInRoleAsync(role)).Count;

    public async Task AddToRoleAsync(Guid id, string role, CancellationToken ct)
        => await userManager.AddToRoleAsync(await GetAsync(id), role);

    public async Task RemoveFromRoleAsync(Guid id, string role, CancellationToken ct)
        => await userManager.RemoveFromRoleAsync(await GetAsync(id), role);

    public async Task SetLockedAsync(Guid id, bool locked, CancellationToken ct)
    {
        var user = await GetAsync(id);
        if (locked)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
        => await userManager.DeleteAsync(await GetAsync(id));

    private async Task<AppUser> GetAsync(Guid id)
        => await userManager.FindByIdAsync(id.ToString())
           ?? throw new NotFoundException("User", id);

    private async Task<UserSummary> ToSummaryAsync(AppUser user)
        => new(user.Id, user.UserName!, user.Email,
            (await userManager.GetRolesAsync(user)).ToList(),
            await userManager.IsLockedOutAsync(user));
}
