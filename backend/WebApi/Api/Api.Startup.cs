using System.Security.Claims;

namespace WebApi.Api;

public static class Startup
{
    internal static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapSolicitationEndpoints();
        
        app.MapGet("/", () => "ok");
        app.MapGet("/api/me", (ClaimsPrincipal user) => user.Identity!.Name).RequireAuthorization();
        
        return app;
    }
}