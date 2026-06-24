using Application;
using Infrastructure;
using WebApi.Api;
using WebApi.Errors;
using WebApi.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidators();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

var admin = app.MapGroup("/api/admin").RequireAuthorization("Admin");

app.Run();