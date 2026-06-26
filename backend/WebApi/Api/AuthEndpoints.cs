using Application.Auth;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace WebApi.Api;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public static class AuthEndpoints
{
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
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>()
            .RequireRateLimiting("login")
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>()
            .AllowAnonymous();

        return app;
    }
    
    private static async Task<IResult> LoginAsync(
        LoginRequest body,
        IAuthenticationService auth,
        CancellationToken ct)
    {
        var result = await auth.LoginAsync(body.Username, body.Password, ct);

        return result.Succeeded
            ? Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt, refreshToken = result.RefreshToken })
            : Results.Unauthorized();
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
        RefreshRequest body,
        IAuthenticationService auth,
        CancellationToken ct)
    {
        var result = await auth.RefreshAsync(body.RefreshToken, ct);

        return result.Succeeded
            ? Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt, refreshToken = result.RefreshToken })
            : Results.Unauthorized();
    }

    private static async Task<IResult> LogoutAsync(
        RefreshRequest body,
        IAuthenticationService auth,
        CancellationToken ct)
    {
        await auth.LogoutAsync(body.RefreshToken, ct);
        return Results.NoContent();
    }
}