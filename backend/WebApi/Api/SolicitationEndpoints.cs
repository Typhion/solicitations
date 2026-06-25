using Application.Solicitations;

namespace WebApi.Api;

public static class SolicitationEndpoints
{
    public static IEndpointRouteBuilder MapSolicitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/solicitations")
            .RequireAuthorization("CanManageSolicitations")
            .WithTags("Solicitations");

        group.MapGet("/", async (SolicitationService service, CancellationToken ct, int page = 1, int pageSize = 20) =>
            Results.Ok(await service.ListAsync(page, pageSize, ct)));

        group.MapGet("/{id:guid}", async (Guid id, SolicitationService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, ct)));

        group.MapPost("/", async (CreateSolicitationRequest body, SolicitationService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(body, ct);
                return Results.Created($"/api/solicitations/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<CreateSolicitationRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, UpdateSolicitationRequest body, SolicitationService service, CancellationToken ct) =>
            {
                await service.UpdateAsync(id, body, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<UpdateSolicitationRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, SolicitationService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        return app;
    }
}