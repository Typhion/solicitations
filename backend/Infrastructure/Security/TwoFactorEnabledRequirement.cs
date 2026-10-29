using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

/// <summary>The signed-in user must currently have two-factor authentication enabled.</summary>
public sealed class TwoFactorEnabledRequirement : IAuthorizationRequirement
{
    public const string FailureMessage = "Enable two-factor authentication on your profile to manage users.";
}

/// <summary>
/// Reads the flag from the database rather than the JWT, so turning 2FA on (or off) takes
/// effect immediately instead of after the next token refresh.
/// </summary>
internal sealed class TwoFactorEnabledHandler(UserManager<AppUser> userManager)
    : AuthorizationHandler<TwoFactorEnabledRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TwoFactorEnabledRequirement requirement)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = id is null ? null : await userManager.FindByIdAsync(id);

        if (user is not null && await userManager.GetTwoFactorEnabledAsync(user))
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(this, TwoFactorEnabledRequirement.FailureMessage));
    }
}