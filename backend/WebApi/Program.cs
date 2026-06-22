using System.Security.Claims;
using Infrastructure;
using WebApi.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "ok");
app.MapAuthEndpoints();
app.MapGet("/api/me", (ClaimsPrincipal user) => user.Identity!.Name).RequireAuthorization();

app.Run();