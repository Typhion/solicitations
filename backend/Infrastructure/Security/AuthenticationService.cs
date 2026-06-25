using Application.Auth;
using Application.Auth.Login;
using Application.Common;
using Domain.Auth;
using Infrastructure.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

internal sealed class AuthenticationService(
    UserManager<AppUser> userManager,
    IJwtTokenService tokenService,
    IRefreshTokenRepository refreshTokens,
    ISecureTokenService secureTokens,
    IOptions<JwtSettings> jwtOptions) : IAuthenticationService
{
    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null) return LoginResult.Fail("Invalid username or password.");

        if (await userManager.IsLockedOutAsync(user)) return LoginResult.Fail("Account locked. Try again later.");

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return LoginResult.Fail("Invalid username or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);
        var refresh = AddRefreshToken(user, DateTime.UtcNow);
        await refreshTokens.SaveChangesAsync(ct);
        return LoginResult.Success(token, expiresAt, refresh);
    }

    public async Task<LoginResult> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var stored = await refreshTokens.GetByTokenHashAsync(secureTokens.Hash(refreshToken), ct);
        if (stored is null)
            return LoginResult.Fail("Invalid refresh token.");

        if (!stored.IsActive(now))
        {
            foreach (var active in await refreshTokens.GetActiveByUserAsync(stored.UserId, ct))
                active.Revoke(now);
            await refreshTokens.SaveChangesAsync(ct);
            return LoginResult.Fail("Invalid refresh token.");
        }

        stored.Revoke(now);
        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await refreshTokens.SaveChangesAsync(ct);
            return LoginResult.Fail("Invalid refresh token.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);
        var newRefresh = AddRefreshToken(user, now);
        await refreshTokens.SaveChangesAsync(ct);
        return LoginResult.Success(token, expiresAt, newRefresh);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var stored = await refreshTokens.GetByTokenHashAsync(secureTokens.Hash(refreshToken), ct);
        if (stored is not null)
        {
            stored.Revoke(DateTime.UtcNow);
            await refreshTokens.SaveChangesAsync(ct);
        }
    }

    private string AddRefreshToken(AppUser user, DateTime now)
    {
        var raw = secureTokens.Generate();
        refreshTokens.Add(new RefreshToken(user.Id, secureTokens.Hash(raw), now.AddDays(jwtOptions.Value.RefreshTokenDays)));
        return raw;
    }
}