using Application.Auth;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace WebApi.Api;

public sealed record LoginRequest(string Username, string Password);

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

        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
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
        if (!result.Succeeded)
            return Results.Unauthorized();

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
        var refreshToken = request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.Unauthorized();

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

    private static void SetRefreshCookie(HttpResponse response, string token, IWebHostEnvironment env, JwtSettings jwt) =>
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
