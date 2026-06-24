using Application.Invites;

namespace WebApi.Api;

public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invites")
            .RequireAuthorization("Admin")
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