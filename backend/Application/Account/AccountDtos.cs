namespace Application.Account;

public sealed record ProfileResponse(
    Guid Id, string Username, string? Email, IReadOnlyList<string> Roles, bool TwoFactorEnabled, int RecoveryCodesLeft);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Key + otpauth:// URI for an authenticator app (Google Authenticator, Microsoft Authenticator, 1Password, …).</summary>
public sealed record AuthenticatorSetupResponse(string SharedKey, string AuthenticatorUri);

public sealed record EnableTwoFactorRequest(string Code);

/// <summary>Re-confirms the password for sensitive actions (disable 2FA, regenerate recovery codes).</summary>
public sealed record PasswordConfirmationRequest(string Password);

public sealed record SetLanguageRequest(string Language);

public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

public sealed record AccountResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static AccountResult Success() => new(true, []);
    public static AccountResult Fail(params IReadOnlyList<string> errors) => new(false, errors);
}

public sealed record AccountResult<T>(bool Succeeded, T? Value, IReadOnlyList<string> Errors)
{
    public static AccountResult<T> Success(T value) => new(true, value, []);
    public static AccountResult<T> Fail(params IReadOnlyList<string> errors) => new(false, default, errors);
}
