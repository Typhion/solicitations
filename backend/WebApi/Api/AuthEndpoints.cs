using Application.Auth;

namespace WebApi.Api;

public sealed record LoginRequest(string Username, string Password);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .RequireRateLimiting("login")
            .AllowAnonymous();

        group.MapPost("/register", RegisterAsync)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .RequireRateLimiting("login")
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
            ? Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt })
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
    
}