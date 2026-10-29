using Application.Auth.Login;

namespace Application.Auth;

public interface IAuthenticationService
{
    /// <summary>Password step. Returns a two-factor challenge instead of a session when 2FA is on.</summary>
    Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct);

    /// <summary>Second step: exchanges a challenge + authenticator (or recovery) code for a session.</summary>
    Task<LoginResult> LoginWithTwoFactorAsync(string challengeToken, string code, bool isRecoveryCode, CancellationToken ct);

    Task<LoginResult> RefreshAsync(string refreshToken, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);

    /// <summary>Revokes all of the user's sessions and issues a new one (e.g. after a password change).</summary>
    Task<LoginResult> ReissueAsync(Guid userId, CancellationToken ct);
}
