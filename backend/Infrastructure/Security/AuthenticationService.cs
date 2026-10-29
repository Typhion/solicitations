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

        // Second factor required: don't reset the failed count yet, or someone holding the password
        // could reset the lockout counter between code guesses.
        if (await userManager.GetTwoFactorEnabledAsync(user))
            return LoginResult.TwoFactorRequired(
                tokenService.CreateTwoFactorChallenge(user.Id, await userManager.GetSecurityStampAsync(user)));

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueSessionAsync(user, ct);
    }

    public async Task<LoginResult> LoginWithTwoFactorAsync(string challengeToken, string code, bool isRecoveryCode, CancellationToken ct)
    {
        var challenge = tokenService.ReadTwoFactorChallenge(challengeToken);
        if (challenge is null) return LoginResult.Fail("Sign-in expired. Please start again.");

        var user = await userManager.FindByIdAsync(challenge.Value.UserId.ToString());
        if (user is null
            || !await userManager.GetTwoFactorEnabledAsync(user)
            || await userManager.GetSecurityStampAsync(user) != challenge.Value.SecurityStamp)
            return LoginResult.Fail("Sign-in expired. Please start again.");

        if (await userManager.IsLockedOutAsync(user)) return LoginResult.Fail("Account locked. Try again later.");

        var verified = isRecoveryCode
            ? await RedeemRecoveryCodeAsync(user, code)
            : await userManager.VerifyTwoFactorTokenAsync(user,
                userManager.Options.Tokens.AuthenticatorTokenProvider, NormalizeTotp(code));

        if (!verified)
        {
            await userManager.AccessFailedAsync(user);
            return LoginResult.Fail("Invalid verification code.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueSessionAsync(user, ct);
    }

    public async Task<LoginResult> ReissueAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new InvalidOperationException($"User {userId} not found.");

        var now = DateTime.UtcNow;
        foreach (var active in await refreshTokens.GetActiveByUserAsync(userId, ct))
            active.Revoke(now);

        return await IssueSessionAsync(user, ct);
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

    private async Task<LoginResult> IssueSessionAsync(AppUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);
        var refresh = AddRefreshToken(user, DateTime.UtcNow);
        await refreshTokens.SaveChangesAsync(ct);
        return LoginResult.Success(token, expiresAt, refresh);
    }

    // Codes look like "XXXXX-XXXXX" (the dash is part of the stored code). A failed redeem doesn't
    // consume anything, so also try the upper-cased input in case it was typed in lower case.
    private async Task<bool> RedeemRecoveryCodeAsync(AppUser user, string code)
    {
        var trimmed = code.Trim();
        if ((await userManager.RedeemTwoFactorRecoveryCodeAsync(user, trimmed)).Succeeded) return true;
        var upper = trimmed.ToUpperInvariant();
        return upper != trimmed && (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, upper)).Succeeded;
    }

    // Authenticator apps often show "123 456"; accept that and "123-456".
    internal static string NormalizeTotp(string code) => code.Replace(" ", "").Replace("-", "");

    private string AddRefreshToken(AppUser user, DateTime now)
    {
        var raw = secureTokens.Generate();
        refreshTokens.Add(new RefreshToken(user.Id, secureTokens.Hash(raw), now.AddDays(jwtOptions.Value.RefreshTokenDays)));
        return raw;
    }
}