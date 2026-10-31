using Application.Auth;
using Application.Auth.Login;
using Application.Common;

namespace Application.Account;

/// <summary>The current user's own account: profile, password and two-factor authentication.</summary>
public sealed class AccountService(IAccountManager accounts, IAuthenticationService auth, ICurrentUser currentUser)
{
    public Task<ProfileResponse> GetProfileAsync(CancellationToken ct)
        => accounts.GetProfileAsync(currentUser.Id, ct);

    /// <summary>
    /// Changes the password, then signs out every other session (all refresh tokens revoked)
    /// and returns a fresh session for the caller so they stay signed in.
    /// </summary>
    public async Task<AccountResult<LoginResult>> ChangePasswordAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var changed = await accounts.ChangePasswordAsync(currentUser.Id, req.CurrentPassword, req.NewPassword, ct);
        if (!changed.Succeeded)
            return AccountResult<LoginResult>.Fail(changed.Errors);

        return AccountResult<LoginResult>.Success(await auth.ReissueAsync(currentUser.Id, ct));
    }

    public Task SetLanguageAsync(SetLanguageRequest req, CancellationToken ct)
        => accounts.SetLanguageAsync(currentUser.Id, req.Language, ct);

    public Task<AccountResult<AuthenticatorSetupResponse>> BeginAuthenticatorSetupAsync(CancellationToken ct)
        => accounts.BeginAuthenticatorSetupAsync(currentUser.Id, ct);

    public Task<AccountResult<IReadOnlyList<string>>> EnableTwoFactorAsync(EnableTwoFactorRequest req, CancellationToken ct)
        => accounts.EnableTwoFactorAsync(currentUser.Id, req.Code, ct);

    public Task<AccountResult> DisableTwoFactorAsync(PasswordConfirmationRequest req, CancellationToken ct)
        => accounts.DisableTwoFactorAsync(currentUser.Id, req.Password, ct);

    public Task<AccountResult<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(PasswordConfirmationRequest req, CancellationToken ct)
        => accounts.RegenerateRecoveryCodesAsync(currentUser.Id, req.Password, ct);
}