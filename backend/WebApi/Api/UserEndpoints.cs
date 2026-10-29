using Application.Users;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace WebApi.Api;

public sealed record GrantRoleRequest(string Role);

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        // Listing only needs Admin; every change needs the admin's 2FA (see InviteEndpoints).
        var group = app.MapGroup("/api/users")
            .RequireAuthorization("Admin")
            .WithApiVersionSet(versionSet)
            .WithTags("Users");

        group.MapGet("/", async (UserManagementService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        group.MapPost("/{id:guid}/roles", async (Guid id, GrantRoleRequest body, UserManagementService service, CancellationToken ct) =>
            {
                await service.GrantRoleAsync(id, body.Role, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<GrantRoleRequest>>()
            .RequireAuthorization("AdminWithTwoFactor");

        group.MapDelete("/{id:guid}/roles/{role}", async (Guid id, string role, UserManagementService service, CancellationToken ct) =>
            {
                await service.RevokeRoleAsync(id, role, ct);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminWithTwoFactor");

        group.MapPost("/{id:guid}/lock", async (Guid id, UserManagementService service, CancellationToken ct) =>
            {
                await service.SetLockedAsync(id, true, ct);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminWithTwoFactor");

        group.MapPost("/{id:guid}/unlock", async (Guid id, UserManagementService service, CancellationToken ct) =>
            {
                await service.SetLockedAsync(id, false, ct);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminWithTwoFactor");

        // POST (not DELETE) because it carries a body: the typed username + the admin's 2FA code.
        group.MapPost("/{id:guid}/delete", async (Guid id, DeleteUserRequest body, UserManagementService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, body, ct);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<DeleteUserRequest>>()
            .RequireAuthorization("AdminWithTwoFactor")
            .RequireRateLimiting("login");

        return app;
    }
}
