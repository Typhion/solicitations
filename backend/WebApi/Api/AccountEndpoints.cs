using Application.Account;
using Asp.Versioning.Builder;
using Infrastructure.Security;
using Microsoft.Extensions.Options;
using WebApi.Errors;

namespace WebApi.Api;

/// <summary>
/// The signed-in user's own account. Lives outside /api/auth so the SPA attaches the bearer
/// token. Expected failures (wrong password, bad code) are 400 — never 401, which the SPA
/// treats as an expired session.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/account")
            .RequireAuthorization()
            .WithApiVersionSet(versionSet)
            .WithTags("Account");

        group.MapGet("/", async (AccountService service, CancellationToken ct) =>
            Results.Ok(await service.GetProfileAsync(ct)));

        group.MapPost("/password", ChangePasswordAsync)
            .AddEndpointFilter<ValidationFilter<ChangePasswordRequest>>()
            .RequireRateLimiting("login");

        group.MapPut("/language", async (SetLanguageRequest body, AccountService service, CancellationToken ct) =>
            {
                await service.SetLanguageAsync(body, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<SetLanguageRequest>>();

        group.MapPost("/2fa/setup", async (AccountService service, CancellationToken ct) =>
            ToResult(await service.BeginAuthenticatorSetupAsync(ct), Results.Ok));

        group.MapPost("/2fa/enable", async (EnableTwoFactorRequest body, AccountService service, CancellationToken ct) =>
                ToResult(await service.EnableTwoFactorAsync(body, ct), codes => Results.Ok(new RecoveryCodesResponse(codes))))
            .AddEndpointFilter<ValidationFilter<EnableTwoFactorRequest>>()
            .RequireRateLimiting("login");

        group.MapPost("/2fa/disable", async (PasswordConfirmationRequest body, AccountService service, CancellationToken ct) =>
            {
                var result = await service.DisableTwoFactorAsync(body, ct);
                return result.Succeeded ? Results.NoContent() : Problem(result.Errors);
            })
            .AddEndpointFilter<ValidationFilter<PasswordConfirmationRequest>>()
            .RequireRateLimiting("login");

        group.MapPost("/2fa/recovery-codes", async (PasswordConfirmationRequest body, AccountService service, CancellationToken ct) =>
                ToResult(await service.RegenerateRecoveryCodesAsync(body, ct), codes => Results.Ok(new RecoveryCodesResponse(codes))))
            .AddEndpointFilter<ValidationFilter<PasswordConfirmationRequest>>()
            .RequireRateLimiting("login");

        return app;
    }

    // Other sessions are revoked; the caller gets a fresh access token + refresh cookie.
    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest body,
        AccountService service,
        HttpResponse response,
        IWebHostEnvironment env,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var result = await service.ChangePasswordAsync(body, ct);
        if (!result.Succeeded)
            return Problem(result.Errors);

        var session = result.Value!;
        AuthEndpoints.SetRefreshCookie(response, session.RefreshToken!, env, jwtOptions.Value);
        return Results.Ok(new { token = session.Token, expiresAt = session.ExpiresAt });
    }

    private static IResult ToResult<T>(AccountResult<T> result, Func<T, IResult> onSuccess)
        => result.Succeeded ? onSuccess(result.Value!) : Problem(result.Errors);

    private static IResult Problem(IReadOnlyList<string> errors)
        => LocalizedProblem.For(errors, StatusCodes.Status400BadRequest);
}
