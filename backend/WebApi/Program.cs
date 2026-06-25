using Application;
using Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Api;
using WebApi.Cors;
using WebApi.Errors;
using WebApi.Security;
using WebApi.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = builder.Configuration.GetValue("RateLimiting:LoginPermitLimit", 5);
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidators();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSecurity();
builder.Services.AddCorsPolicies(builder.Configuration);

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseExceptionHandler();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapEndpoints();

var admin = app.MapGroup("/api/admin").RequireAuthorization("Admin");

app.Run();

// Exposed so the integration test project can use WebApplicationFactory<Program>.
public partial class Program;