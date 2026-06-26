using Application.Invites;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace WebApi.Api;

public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/invites")
            .RequireAuthorization("Admin")
            .WithApiVersionSet(versionSet)
            .WithTags("Invites");

        group.MapPost("/", async (CreateInviteRequest body, InviteService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(body, ct);
                return Results.Created($"/api/invites/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<CreateInviteRequest>>();

        group.MapGet("/", async (InviteService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        group.MapPost("/{id:guid}/revoke", async (Guid id, InviteService service, CancellationToken ct) =>
        {
            await service.RevokeAsync(id, ct);
            return Results.NoContent();
        });

        return app;
    }
}