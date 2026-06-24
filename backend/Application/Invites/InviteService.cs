using Application.Common;
using Application.Common.Exceptions;
using Domain.Invites;

namespace Application.Invites;

public sealed class InviteService(
    IInviteRepository repository, IInviteTokenService tokens, ICurrentUser currentUser)
{
    public async Task<CreatedInviteResponse> CreateAsync(CreateInviteRequest req, CancellationToken ct)
    {
        var token = tokens.Generate();
        var invite = new Invite(tokens.Hash(token), req.Role,
            DateTime.UtcNow.AddDays(req.ExpiresInDays ?? 7), currentUser.Id, req.Email);
        repository.Add(invite);
        await repository.SaveChangesAsync(ct);
        return new CreatedInviteResponse(invite.Id, token, invite.ExpiresAtUtc);
    }

    public async Task<IReadOnlyList<InviteResponse>> ListAsync(CancellationToken ct)
        => (await repository.ListAsync(ct)).Select(i => i.ToResponse()).ToList();

    public async Task RevokeAsync(Guid id, CancellationToken ct)
    {
        var invite = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Invite), id);
        invite.Revoke();
        await repository.SaveChangesAsync(ct);
    }
}