using System.Threading.RateLimiting;
using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using WebApi.Api;
using WebApi.Cors;
using WebApi.Errors;
using WebApi.Security;
using WebApi.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Per-client bucket (partitioned by IP) instead of one global window.
    var loginPermit = builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5);
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidators();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSecurity();
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SolicitationsDbContext>("database", tags: ["ready"]);

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

var admin = app.MapGroup("/api/admin").RequireAuthorization("Admin");

app.Run();

// Exposed so the integration test project can use WebApplicationFactory<Program>.
public partial class Program;