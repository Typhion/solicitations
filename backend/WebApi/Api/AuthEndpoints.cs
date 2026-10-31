using Application.Auth;
using Application.Invites;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace WebApi.Api;

public sealed record LoginRequest(string Username, string Password);

public sealed record TwoFactorLoginRequest(string ChallengeToken, string Code, bool UseRecoveryCode);

public static class AuthEndpoints
{
    private const string RefreshCookieName = "refreshToken";
    // Scope the cookie to the auth endpoints so it is only ever sent to /refresh + /logout.
    private const string RefreshCookiePath = "/api/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/auth").WithApiVersionSet(versionSet).WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .RequireRateLimiting("login")
            .AllowAnonymous();

        group.MapPost("/login/2fa", LoginWithTwoFactorAsync)
            .AddEndpointFilter<ValidationFilter<TwoFactorLoginRequest>>()
            .RequireRateLimiting("login")
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .RequireRateLimiting("login")
            .AllowAnonymous();

        // Lets the register page show/lock the invited email before the user fills anything in.
        group.MapGet("/invites/{token}", async (string token, InviteService invites, CancellationToken ct) =>
                Results.Ok(await invites.LookupAsync(token, ct)))
            .RequireRateLimiting("login")
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshAsync)
            .RequireRateLimiting("login")
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest body,
        IAuthenticationService auth,
        HttpResponse response,
        IWebHostEnvironment env,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var result = await auth.LoginAsync(body.Username, body.Password, ct);
        if (result.RequiresTwoFactor)
            return Results.Ok(new { requiresTwoFactor = true, challengeToken = result.ChallengeToken });
        if (!result.Succeeded)
            return Results.Unauthorized();

        SetRefreshCookie(response, result.RefreshToken!, env, jwtOptions.Value);
        return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }

    private static async Task<IResult> LoginWithTwoFactorAsync(
        TwoFactorLoginRequest body,
        IAuthenticationService auth,
        HttpResponse response,
        IWebHostEnvironment env,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var result = await auth.LoginWithTwoFactorAsync(body.ChallengeToken, body.Code, body.UseRecoveryCode, ct);
        if (!result.Succeeded)
            return Results.Problem(detail: result.Error, statusCode: StatusCodes.Status401Unauthorized);

        SetRefreshCookie(response, result.RefreshToken!, env, jwtOptions.Value);
        return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest body,
        RegistrationService registration,
        CancellationToken ct)
    {
        var result = await registration.RegisterAsync(body, ct);

        return result.Succeeded
            ? Results.Created($"/api/users/{body.Username}", null)
            : Results.Problem(detail: string.Join("; ", result.Errors), statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RefreshAsync(
        IAuthenticationService auth,
        HttpRequest request,
        HttpResponse response,
        IWebHostEnvironment env,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        // No cookie = no session (the SPA probes this on every load). Not an error, so answer
        // 204 instead of 401 to keep the browser console clean. A bad cookie still gets 401.
        var refreshToken = request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.NoContent();

        var result = await auth.RefreshAsync(refreshToken, ct);
        if (!result.Succeeded)
        {
            DeleteRefreshCookie(response, env);
            return Results.Unauthorized();
        }

        SetRefreshCookie(response, result.RefreshToken!, env, jwtOptions.Value);
        return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
    }

    private static async Task<IResult> LogoutAsync(
        IAuthenticationService auth,
        HttpRequest request,
        HttpResponse response,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        var refreshToken = request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
            await auth.LogoutAsync(refreshToken, ct);

        DeleteRefreshCookie(response, env);
        return Results.NoContent();
    }

    internal static void SetRefreshCookie(HttpResponse response, string token, IWebHostEnvironment env, JwtSettings jwt) =>
        response.Cookies.Append(RefreshCookieName, token, BuildCookieOptions(env, DateTimeOffset.UtcNow.AddDays(jwt.RefreshTokenDays)));

    private static void DeleteRefreshCookie(HttpResponse response, IWebHostEnvironment env) =>
        // Must reuse the same Path/attributes or the browser won't clear it.
        response.Cookies.Delete(RefreshCookieName, BuildCookieOptions(env, DateTimeOffset.UnixEpoch));

    private static CookieOptions BuildCookieOptions(IWebHostEnvironment env, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = !env.IsDevelopment(), // dev runs over http://localhost, where Secure cookies are dropped
        SameSite = SameSiteMode.Lax,   // web + api are same-site subdomains of typhion.com
        Path = RefreshCookiePath,
        Expires = expires,
    };
}
