using Domain.Core;

namespace Domain.Invites;

public class Invite : Entity
{
    private Invite() { }

    public Invite(string tokenHash, string role, DateTime expiresAtUtc, Guid createdByUserId, string? email = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Creator is required.", nameof(createdByUserId));

        TokenHash = tokenHash;
        Role = role;
        Email = email;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        Status = InviteStatus.Pending;
    }

    public string TokenHash { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public string? Email { get; private set; }
    public InviteStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }

    public bool IsRedeemable(DateTime utcNow) => Status == InviteStatus.Pending && utcNow < ExpiresAtUtc;

    public void Redeem(DateTime utcNow)
    {
        if (!IsRedeemable(utcNow)) throw new DomainException("Invite is not redeemable.");
        Status = InviteStatus.Accepted;
        AcceptedAtUtc = utcNow;
    }

    public void Revoke()
    {
        if (Status != InviteStatus.Pending) throw new DomainException("Only pending invites can be revoked.");
        Status = InviteStatus.Revoked;
    }
}