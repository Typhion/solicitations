using Application.Users;

namespace WebApi.Api;

public sealed record GrantRoleRequest(string Role);

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization("Admin")
            .WithTags("Users");

        group.MapGet("/", async (UserManagementService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        group.MapPost("/{id:guid}/roles", async (Guid id, GrantRoleRequest body, UserManagementService service, CancellationToken ct) =>
            {
                await service.GrantRoleAsync(id, body.Role, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<GrantRoleRequest>>();

        group.MapDelete("/{id:guid}/roles/{role}", async (Guid id, string role, UserManagementService service, CancellationToken ct) =>
            {
                await service.RevokeRoleAsync(id, role, ct);
                return Results.NoContent();
            });

        group.MapPost("/{id:guid}/lock", async (Guid id, UserManagementService service, CancellationToken ct) =>
            {
                await service.SetLockedAsync(id, true, ct);
                return Results.NoContent();
            });

        group.MapPost("/{id:guid}/unlock", async (Guid id, UserManagementService service, CancellationToken ct) =>
            {
                await service.SetLockedAsync(id, false, ct);
                return Results.NoContent();
            });

        group.MapDelete("/{id:guid}", async (Guid id, UserManagementService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            });

        return app;
    }
}
