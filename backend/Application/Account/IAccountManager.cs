namespace Application.Account;

/// <summary>
/// Self-service account operations backed by the identity store. Throws
/// <see cref="Common.Exceptions.NotFoundException"/> when the user doesn't exist;
/// expected failures (wrong password, invalid code, policy violations) are returned.
/// </summary>
public interface IAccountManager
{
    Task<ProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct);
    Task<AccountResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);

    /// <summary>Generates a fresh authenticator key. Only allowed while 2FA is off.</summary>
    Task<AccountResult<AuthenticatorSetupResponse>> BeginAuthenticatorSetupAsync(Guid userId, CancellationToken ct);

    /// <summary>Verifies a code from the app, turns 2FA on and returns new recovery codes.</summary>
    Task<AccountResult<IReadOnlyList<string>>> EnableTwoFactorAsync(Guid userId, string code, CancellationToken ct);

    Task<AccountResult> DisableTwoFactorAsync(Guid userId, string password, CancellationToken ct);
    Task<AccountResult<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(Guid userId, string password, CancellationToken ct);

    /// <summary>
    /// Step-up check for sensitive actions: true when 2FA is on and the code from the user's
    /// authenticator app is valid. Wrong codes count toward lockout.
    /// </summary>
    Task<bool> VerifyAuthenticatorCodeAsync(Guid userId, string code, CancellationToken ct);
}