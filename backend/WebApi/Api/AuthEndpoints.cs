using Application.Auth.Login;
using MediatR;

namespace WebApi.Api;

public sealed record LoginRequest(string Username, string Password);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LoginCommand(body.Username, body.Password), ct);
            return result.Succeeded
                ? Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt })
                : Results.Unauthorized();
        });

        return app;
    }
}