using Domain.Invites;

namespace Application.Invites;

internal static class InviteMappings
{
    public static InviteResponse ToResponse(this Invite i, DateTime utcNow)
        => new(i.Id, i.Email, i.Role, i.Status,
            IsExpired: i.Status == InviteStatus.Pending && utcNow >= i.ExpiresAtUtc,
            i.ExpiresAtUtc, i.CreatedAtUtc);
}