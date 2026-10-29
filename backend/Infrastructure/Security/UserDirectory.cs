using Application.Auth;
using Application.Common.Exceptions;
using Application.Users;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

internal sealed class UserDirectory(
    UserManager<AppUser> userManager,
    IRefreshTokenRepository refreshTokens,
    SolicitationsDbContext context) : IUserDirectory
{
    public async Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct)
    {
        var users = await userManager.Users.OrderBy(u => u.UserName).ToListAsync(ct);
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

    // Identity looks this up by its normalized (upper-cased) email.
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
        => await userManager.FindByEmailAsync(email) is not null;

    public async Task<int> CountInRoleAsync(string role, CancellationToken ct)
        => (await userManager.GetUsersInRoleAsync(role)).Count;

    // Identity doesn't rotate the security stamp on role changes; do it so tokens carrying the
    // old roles are rejected right away (see AccessTokenValidator). The user's next refresh
    // gets a token with the new roles.
    public async Task AddToRoleAsync(Guid id, string role, CancellationToken ct)
    {
        var user = await GetAsync(id);
        await userManager.AddToRoleAsync(user, role);
        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task RemoveFromRoleAsync(Guid id, string role, CancellationToken ct)
    {
        var user = await GetAsync(id);
        await userManager.RemoveFromRoleAsync(user, role);
        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task SetLockedAsync(Guid id, bool locked, CancellationToken ct)
    {
        var user = await GetAsync(id);
        if (locked)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            // Refresh already refuses locked users; revoking ends their sessions explicitly.
            var now = DateTime.UtcNow;
            foreach (var token in await refreshTokens.GetActiveByUserAsync(id, ct))
                token.Revoke(now);
            await refreshTokens.SaveChangesAsync(ct);
        }
        else
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }
    }

    public async Task DeleteWithDataAsync(Guid id, CancellationToken ct)
    {
        var user = await GetAsync(id);

        // Solicitations and refresh tokens reference the user by id only (no FK), so remove them
        // explicitly. UserManager shares this scoped DbContext, so everything is in one transaction.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        // Meetings go with their solicitation via the database's cascade delete.
        await context.Solicitations.Where(s => s.OwnerId == id).ExecuteDeleteAsync(ct);
        await context.RefreshTokens.Where(t => t.UserId == id).ExecuteDeleteAsync(ct);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        await transaction.CommitAsync(ct);
    }

    private async Task<AppUser> GetAsync(Guid id)
        => await userManager.FindByIdAsync(id.ToString())
           ?? throw new NotFoundException("User", id);

    private async Task<UserSummary> ToSummaryAsync(AppUser user)
        => new(user.Id, user.UserName!, user.Email,
            (await userManager.GetRolesAsync(user)).ToList(),
            await userManager.IsLockedOutAsync(user),
            await userManager.GetTwoFactorEnabledAsync(user));
}