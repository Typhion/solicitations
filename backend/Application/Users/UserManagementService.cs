using Application.Common;
using Application.Common.Exceptions;
using Domain.Core;

namespace Application.Users;

public sealed class UserManagementService(IUserDirectory directory, ICurrentUser currentUser)
  {
      public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken ct)
          => (await directory.ListAsync(ct))
              .Select(u => new UserResponse(u.Id, u.Username, u.Email, u.Roles, u.IsLockedOut)).ToList();

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

      public async Task DeleteAsync(Guid id, CancellationToken ct)
      {
          if (id == currentUser.Id) throw new DomainException("You cannot delete your own account.");
          var user = await Require(id, ct);
          if (user.Roles.Contains(Roles.Admin) && await directory.CountInRoleAsync(Roles.Admin, ct) <= 1) throw new DomainException("Cannot delete the last administrator.");
          await directory.DeleteAsync(id, ct);
      }

      private async Task<UserSummary> Require(Guid id, CancellationToken ct)
          => await directory.FindAsync(id, ct) ?? throw new NotFoundException("User", id);

      private static void EnsureAssignable(string role)
      {
          if (!Roles.Assignable.Contains(role)) throw new DomainException($"Unknown role '{role}'.");
      }
  }