using Application.Account;
using Application.Common;
using Application.Common.Exceptions;
using Application.Solicitations;
using Domain.Core;

namespace Application.Users;

public sealed class UserManagementService(
    IUserDirectory directory,
    ICurrentUser currentUser,
    ISolicitationRepository solicitations,
    IAccountManager accounts)
{
    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct)
    {
        var users = await directory.ListAsync(ct);
        var responses = new List<UserResponse>(users.Count);
        foreach (var u in users)
            responses.Add(new UserResponse(u.Id, u.Username, u.Email, u.Roles, u.IsLockedOut, u.TwoFactorEnabled,
                await solicitations.CountAsync(u.Id, ct)));
        return responses;
    }

    public async Task GrantRoleAsync(Guid id, string role, CancellationToken ct)
    {
        EnsureAssignable(role);
        _ = await Require(id, ct);
        await directory.AddToRoleAsync(id, role, ct);
    }

    public async Task RevokeRoleAsync(Guid id, string role, CancellationToken ct)
    {
        EnsureAssignable(role);
        _ = await Require(id, ct);
        if (role == Roles.Admin && id == currentUser.Id) throw new DomainException("You cannot remove your own administrator role.");
        if (role == Roles.Admin && await directory.CountInRoleAsync(Roles.Admin, ct) <= 1) throw new DomainException("Cannot remove the last administrator.");
        await directory.RemoveFromRoleAsync(id, role, ct);
    }

    public async Task SetLockedAsync(Guid id, bool locked, CancellationToken ct)
    {
        var user = await Require(id, ct);
        if (locked && id == currentUser.Id) throw new DomainException("You cannot lock your own account.");
        if (locked && user.Roles.Contains(Roles.Admin) && await directory.CountInRoleAsync(Roles.Admin, ct) <= 1) throw new DomainException("Cannot lock the last administrator.");
        await directory.SetLockedAsync(id, locked, ct);
    }

    /// <summary>
    /// Permanently deletes a user and everything they own. Requires the target's username typed
    /// back and the acting admin's current authenticator code.
    /// </summary>
    public async Task DeleteAsync(Guid id, DeleteUserRequest req, CancellationToken ct)
    {
        if (id == currentUser.Id) throw new DomainException("You cannot delete your own account.");
        var user = await Require(id, ct);
        if (!string.Equals(user.Username, req.Username.Trim(), StringComparison.Ordinal))
            throw new DomainException("The username doesn't match the user you're deleting.");
        if (user.Roles.Contains(Roles.Admin) && await directory.CountInRoleAsync(Roles.Admin, ct) <= 1) throw new DomainException("Cannot delete the last administrator.");

        // Last, so cheap mistakes don't burn lockout attempts.
        if (!await accounts.VerifyAuthenticatorCodeAsync(currentUser.Id, req.Code, ct))
            throw new DomainException("Invalid authentication code.");

        await directory.DeleteWithDataAsync(id, ct);
    }

    private async Task<UserSummary> Require(Guid id, CancellationToken ct)
        => await directory.FindAsync(id, ct) ?? throw new NotFoundException("User", id);

    private static void EnsureAssignable(string role)
    {
        if (!Roles.Assignable.Contains(role)) throw new DomainException($"Unknown role '{role}'.");
    }
}