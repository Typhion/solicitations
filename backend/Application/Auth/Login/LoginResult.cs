namespace Application.Auth.Login;

public sealed record LoginResult(bool Succeeded, string? Token, DateTime? ExpiresAt, string? RefreshToken, string? Error)
{
    public static LoginResult Success(string token, DateTime expiresAt, string refreshToken) => new(true, token, expiresAt, refreshToken, null);
    public static LoginResult Fail(string error) => new(false, null, null, null, error);
}
