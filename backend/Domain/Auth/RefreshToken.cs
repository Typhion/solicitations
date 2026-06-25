using Domain.Core;

namespace Domain.Auth;

public class RefreshToken : Entity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && utcNow < ExpiresAtUtc;

    public void Revoke(DateTime utcNow)
    {
        if (RevokedAtUtc is null) RevokedAtUtc = utcNow;
    }
}