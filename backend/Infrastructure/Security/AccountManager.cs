using System.Text;
using Application.Account;
using Application.Common.Exceptions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

internal sealed class AccountManager(UserManager<AppUser> userManager) : IAccountManager
{
    // Shown as the account name in the authenticator app.
    private const string Issuer = "Solicitations";
    private const int RecoveryCodeCount = 10;

    public async Task<ProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        return new ProfileResponse(
            user.Id,
            user.UserName!,
            user.Email,
            (await userManager.GetRolesAsync(user)).ToList(),
            await userManager.GetTwoFactorEnabledAsync(user),
            await userManager.CountRecoveryCodesAsync(user));
    }

    public async Task<AccountResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        if (await userManager.IsLockedOutAsync(user)) return AccountResult.Fail("Account locked. Try again later.");

        // Also enforces the password policy and updates the security stamp.
        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded) return AccountResult.Success();

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            await userManager.AccessFailedAsync(user);
        return AccountResult.Fail(result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<AccountResult<AuthenticatorSetupResponse>> BeginAuthenticatorSetupAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        if (await userManager.GetTwoFactorEnabledAsync(user))
            return AccountResult<AuthenticatorSetupResponse>.Fail("Two-factor authentication is already enabled.");

        // Always start from a fresh key so an abandoned/leaked earlier setup can't be completed.
        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = (await userManager.GetAuthenticatorKeyAsync(user))!;

        return AccountResult<AuthenticatorSetupResponse>.Success(
            new AuthenticatorSetupResponse(FormatKey(key), AuthenticatorUri(user.UserName!, key)));
    }

    public async Task<AccountResult<IReadOnlyList<string>>> EnableTwoFactorAsync(Guid userId, string code, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        if (await userManager.GetTwoFactorEnabledAsync(user))
            return AccountResult<IReadOnlyList<string>>.Fail("Two-factor authentication is already enabled.");
        if (string.IsNullOrEmpty(await userManager.GetAuthenticatorKeyAsync(user)))
            return AccountResult<IReadOnlyList<string>>.Fail("Start the authenticator setup first.");

        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, AuthenticationService.NormalizeTotp(code));
        if (!valid)
            return AccountResult<IReadOnlyList<string>>.Fail("Invalid verification code. Check the time on your phone and try again.");

        await userManager.SetTwoFactorEnabledAsync(user, true);
        return AccountResult<IReadOnlyList<string>>.Success(await NewRecoveryCodesAsync(user));
    }

    public async Task<AccountResult> DisableTwoFactorAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        var confirmed = await ConfirmPasswordAsync(user, password);
        if (!confirmed.Succeeded) return confirmed;

        await userManager.SetTwoFactorEnabledAsync(user, false);
        // Invalidate the old key: re-enabling requires scanning a new one.
        await userManager.ResetAuthenticatorKeyAsync(user);
        return AccountResult.Success();
    }

    public async Task<AccountResult<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        if (!await userManager.GetTwoFactorEnabledAsync(user))
            return AccountResult<IReadOnlyList<string>>.Fail("Two-factor authentication is not enabled.");

        var confirmed = await ConfirmPasswordAsync(user, password);
        if (!confirmed.Succeeded) return AccountResult<IReadOnlyList<string>>.Fail(confirmed.Errors);

        return AccountResult<IReadOnlyList<string>>.Success(await NewRecoveryCodesAsync(user));
    }

    public async Task<bool> VerifyAuthenticatorCodeAsync(Guid userId, string code, CancellationToken ct)
    {
        var user = await GetAsync(userId);
        if (!await userManager.GetTwoFactorEnabledAsync(user) || await userManager.IsLockedOutAsync(user))
            return false;

        if (await userManager.VerifyTwoFactorTokenAsync(
                user, userManager.Options.Tokens.AuthenticatorTokenProvider, AuthenticationService.NormalizeTotp(code)))
            return true;

        await userManager.AccessFailedAsync(user);
        return false;
    }

    // Wrong passwords count toward lockout, same as at login.
    private async Task<AccountResult> ConfirmPasswordAsync(AppUser user, string password)
    {
        if (await userManager.IsLockedOutAsync(user)) return AccountResult.Fail("Account locked. Try again later.");
        if (await userManager.CheckPasswordAsync(user, password)) return AccountResult.Success();

        await userManager.AccessFailedAsync(user);
        return AccountResult.Fail("Incorrect password.");
    }

    private async Task<IReadOnlyList<string>> NewRecoveryCodesAsync(AppUser user)
        => (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount))?.ToList() ?? [];

    private async Task<AppUser> GetAsync(Guid id)
        => await userManager.FindByIdAsync(id.ToString())
           ?? throw new NotFoundException("User", id);

    // "abcdefgh…" → "abcd efgh …" for typing it in by hand.
    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(key.AsSpan(i, Math.Min(4, key.Length - i)));
        }
        return sb.ToString().ToLowerInvariant();
    }

    // https://github.com/google/google-authenticator/wiki/Key-Uri-Format
    private static string AuthenticatorUri(string username, string key)
    {
        var issuer = Uri.EscapeDataString(Issuer);
        return $"otpauth://totp/{issuer}:{Uri.EscapeDataString(username)}?secret={key}&issuer={issuer}&digits=6";
    }
}
