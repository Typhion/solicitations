using System.Security.Claims;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace WebApi.Api;

public static class Startup
{
    internal static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build();

        app.MapAuthEndpoints(versionSet);
        app.MapSolicitationEndpoints(versionSet);
        app.MapInviteEndpoints(versionSet);
        app.MapUserEndpoints(versionSet);

        app.MapGet("/", () => "ok");
        app.MapGet("/api/me", (ClaimsPrincipal user) => user.Identity!.Name).RequireAuthorization();
        
        return app;
    }
}