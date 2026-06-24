using Application.Common;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

internal sealed class UserRegistrar(UserManager<AppUser> userManager) : IUserRegistrar
{
    public async Task<UserRegistrationResult> CreateAsync(string username, string email, string password, string role, CancellationToken ct)
    {
        var user = new AppUser { UserName = username, Email = email };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new(false, result.Errors.Select(e => e.Description).ToList());
        await userManager.AddToRoleAsync(user, role);
        return new(true, []);
    }
}