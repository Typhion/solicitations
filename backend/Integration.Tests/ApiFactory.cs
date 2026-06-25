using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Integration.Tests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL container. Migrations + seeding
/// run on startup (via Program's InitialiseDatabaseAsync), so the admin user exists
/// with <see cref="AdminPassword"/>.
///
/// Config is supplied via environment variables (not ConfigureAppConfiguration) because
/// the app reads Jwt:Key eagerly at registration — env vars are read by CreateBuilder
/// early enough, an in-memory source added in ConfigureWebHost is not.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminPassword = "Admin#Test12345";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _db.StartAsync();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-tests-signing-key-0123456789-abcdefghij");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
        Environment.SetEnvironmentVariable("RateLimiting__LoginPermitLimit", "1000"); // don't 429 the suite
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
