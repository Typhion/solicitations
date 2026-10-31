using System.Threading.RateLimiting;
using Application;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
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
    
    // Per-IP fixed window of one minute.
    void AddPerIpPolicy(string name, int permitLimit) =>
        options.AddPolicy(name, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

    // Tight: guards password guessing.
    AddPerIpPolicy("login", builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5));
    // Loose and separate: the SPA calls /refresh on every page load and on token expiry, so it
    // must not eat into the login budget. Still capped to blunt refresh-token guessing.
    AddPerIpPolicy("refresh", builder.Configuration.GetValue("RateLimiting:RefreshPermitLimit", 60));

    // Per user: address search spends the Google Places quota.
    var placesLimit = builder.Configuration.GetValue("RateLimiting:PlacesPermitLimit", 30);
    options.AddPolicy("places", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                          ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = placesLimit,
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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseForwardedHeaders();
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