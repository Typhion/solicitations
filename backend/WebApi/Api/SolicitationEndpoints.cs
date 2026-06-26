using Application.Solicitations;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace WebApi.Api;

public static class SolicitationEndpoints
{
    public static IEndpointRouteBuilder MapSolicitationEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/solicitations")
            .RequireAuthorization("CanManageSolicitations")
            .WithApiVersionSet(versionSet)
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

        group.MapPost("/{id:guid}/meetings", async (Guid id, AddMeetingRequest body, SolicitationService service, CancellationToken ct) =>
            {
                var updated = await service.AddMeetingAsync(id, body, ct);
                return Results.Ok(updated);
            })
            .AddEndpointFilter<ValidationFilter<AddMeetingRequest>>();

        group.MapDelete("/{id:guid}/meetings/{meetingId:guid}", async (Guid id, Guid meetingId, SolicitationService service, CancellationToken ct) =>
        {
            await service.RemoveMeetingAsync(id, meetingId, ct);
            return Results.NoContent();
        });

        return app;
    }
}