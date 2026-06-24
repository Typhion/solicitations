using Domain.Invites;

namespace Application.Invites;

internal static class InviteMappings
{
    public static InviteResponse ToResponse(this Invite i)
        => new(i.Id, i.Email, i.Role, i.Status, i.ExpiresAtUtc, i.CreatedAtUtc);
}