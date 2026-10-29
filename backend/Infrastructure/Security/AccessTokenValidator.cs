using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security;

/// <summary>
/// Runs after the JWT's signature and lifetime check, on every authenticated request. Access
/// tokens are otherwise trusted until they expire, so without this a locked, deleted or demoted
/// user would keep their access for the rest of the token's lifetime.
///
/// Rejects the token (→ 401) when the user no longer exists, is locked out, or their security
/// stamp changed since it was issued — Identity rotates the stamp on password changes, role
/// changes and authenticator resets. The SPA then tries a refresh: that works for a legitimate
/// change (new token, new roles) and fails for a locked/deleted user, who is signed out.
/// Costs one user lookup per request.
/// </summary>
internal static class AccessTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = principal?.FindFirstValue(JwtTokenService.SecurityStampClaim);

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
        var user = id is null ? null : await userManager.FindByIdAsync(id);

        if (user is null)
            context.Fail("User no longer exists.");
        else if (await userManager.IsLockedOutAsync(user))
            context.Fail("Account is locked.");
        else if (stamp is null || stamp != await userManager.GetSecurityStampAsync(user))
            context.Fail("Token is out of date.");
    }
}