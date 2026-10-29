using Application.Common;
using Application.Common.Exceptions;
using Application.Users;
using Domain.Core;
using Domain.Invites;

namespace Application.Invites;

public sealed class InviteService(
    IInviteRepository repository,
    ISecureTokenService tokens,
    ICurrentUser currentUser,
    IUserDirectory users,
    IInviteNotifier notifier)
{
    private const int DefaultExpiryDays = 7;

    public async Task<CreatedInviteResponse> CreateAsync(CreateInviteRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim();
        var now = DateTime.UtcNow;

        if (await users.ExistsByEmailAsync(email, ct))
            throw new DomainException("A user with this email address already exists.");
        if (await repository.HasOpenInviteForEmailAsync(email, now, ct))
            throw new DomainException("This email address already has a pending invite. Resend it instead.");

        var token = tokens.Generate();
        // Invites only ever create normal users; admins are promoted from the users screen.
        var invite = new Invite(tokens.Hash(token), Roles.Member,
            now.AddDays(req.ExpiresInDays ?? DefaultExpiryDays), currentUser.Id, email);
        repository.Add(invite);
        await repository.SaveChangesAsync(ct);

        return await SendAsync(invite, token, ct);
    }

    /// <summary>New token + fresh expiry, emailed again. The previous link stops working.</summary>
    public async Task<CreatedInviteResponse> ResendAsync(Guid id, CancellationToken ct)
    {
        var invite = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Invite), id);

        var token = tokens.Generate();
        invite.Reissue(tokens.Hash(token), DateTime.UtcNow.AddDays(DefaultExpiryDays));
        await repository.SaveChangesAsync(ct);

        return await SendAsync(invite, token, ct);
    }

    public async Task<IReadOnlyList<InviteResponse>> ListAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return (await repository.ListAsync(ct)).Select(i => i.ToResponse(now)).ToList();
    }

    public async Task RevokeAsync(Guid id, CancellationToken ct)
    {
        var invite = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Invite), id);
        invite.Revoke();
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>For the register page. Unknown, used, revoked and expired tokens all look the same (404).</summary>
    public async Task<InviteLookupResponse> LookupAsync(string token, CancellationToken ct)
    {
        var invite = await repository.GetByTokenHashAsync(tokens.Hash(token), ct);
        if (invite is null || !invite.IsRedeemable(DateTime.UtcNow))
            throw new NotFoundException(nameof(Invite), "token");
        return new InviteLookupResponse(invite.Email, invite.ExpiresAtUtc);
    }

    private async Task<CreatedInviteResponse> SendAsync(Invite invite, string token, CancellationToken ct)
    {
        var link = notifier.CreateLink(token);
        var sent = invite.Email is not null
                   && await notifier.TrySendInviteAsync(invite.Email, link, invite.ExpiresAtUtc, ct);
        return new CreatedInviteResponse(invite.Id, link, invite.ExpiresAtUtc, sent);
    }
}
