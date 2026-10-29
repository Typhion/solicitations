using Infrastructure.Persistence;

namespace Infrastructure.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles);

    /// <summary>
    /// Short-lived token proving the password step succeeded. Uses a separate audience so it
    /// can never be accepted as an access token. Carries the security stamp so it dies if the
    /// user's credentials change in the meantime.
    /// </summary>
    string CreateTwoFactorChallenge(Guid userId, string securityStamp);

    /// <summary>Returns the challenge's user id + security stamp, or null if invalid/expired.</summary>
    (Guid UserId, string SecurityStamp)? ReadTwoFactorChallenge(string challengeToken);
}