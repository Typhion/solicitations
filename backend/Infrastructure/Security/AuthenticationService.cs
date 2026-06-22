using Application.Auth;
using Application.Auth.Login;
using Infrastructure.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

internal sealed class AuthenticationService(
    UserManager<AppUser> userManager,
    IJwtTokenService tokenService) : IAuthenticationService
{
    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return LoginResult.Fail("Invalid username or password.");

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);
        return LoginResult.Success(token, expiresAt);
    }
}