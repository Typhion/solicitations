namespace Application.Auth.Login;

public sealed record LoginResult(
    bool Succeeded,
    string? Token,
    DateTime? ExpiresAt,
    string? RefreshToken,
    string? Error,
    bool RequiresTwoFactor = false,
    string? ChallengeToken = null)
{
    public static LoginResult Success(string token, DateTime expiresAt, string refreshToken) => new(true, token, expiresAt, refreshToken, null);
    public static LoginResult Fail(string error) => new(false, null, null, null, error);

    /// <summary>Password was correct but a second factor is needed; no session is issued yet.</summary>
    public static LoginResult TwoFactorRequired(string challengeToken) => new(false, null, null, null, null, true, challengeToken);
}