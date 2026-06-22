namespace Application.Auth.Login;

public sealed record LoginResult(bool Succeeded, string? Token, DateTime? ExpiresAt, string? Error)
{
    public static LoginResult Success(string token, DateTime expiresAt) => new(true, token, expiresAt, null);
    public static LoginResult Fail(string error) => new(false, null, null, error);
}
