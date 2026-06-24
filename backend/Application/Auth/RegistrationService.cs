using Application.Common;
using Application.Invites;

namespace Application.Auth;

public sealed class RegistrationService(
    IInviteRepository invites, IInviteTokenService tokens, IUserRegistrar registrar)
{
    public async Task<RegisterResult> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var invite = await invites.GetByTokenHashAsync(tokens.Hash(req.Token), ct);
        if (invite is null || !invite.IsRedeemable(now))
            return RegisterResult.Fail("Invalid or expired invite.");   // 🔸 generic

        var created = await registrar.CreateAsync(req.Username, req.Email, req.Password, invite.Role, ct);
        if (!created.Succeeded)
            return RegisterResult.Fail(created.Errors);

        invite.Redeem(now);
        await invites.SaveChangesAsync(ct);
        return RegisterResult.Success();
    }
}