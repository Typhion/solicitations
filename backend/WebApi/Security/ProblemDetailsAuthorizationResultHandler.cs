using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using WebApi.Errors;

namespace WebApi.Security;

/// <summary>
/// Turns a 403 that has an explanation (e.g. "enable 2FA to manage users") into ProblemDetails
/// so the SPA can show it. Everything else uses the default behaviour (plain 401/403).
/// </summary>
internal sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        var reason = result.AuthorizationFailure?.FailureReasons.FirstOrDefault()?.Message;
        if (result.Forbidden && reason is not null)
        {
            await LocalizedProblem.For(reason, StatusCodes.Status403Forbidden).ExecuteAsync(context);
            return;
        }

        await _default.HandleAsync(next, context, policy, result);
    }
}
