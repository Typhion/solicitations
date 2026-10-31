using System.Collections.Concurrent;
using Application.Common;
using Application.Places;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    /// <summary>Every email the API "sends" lands here instead of an SMTP server.</summary>
    public CapturingEmailSender Emails { get; } = new();

    /// <summary>
    /// Admin actions require 2FA. The first test that needs it enables it on the seeded admin
    /// and keeps the authenticator secret here, so later tests can produce codes.
    /// </summary>
    public string? AdminTotpSecret { get; set; }

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
            // never call Google from tests
            services.RemoveAll<IPlacesClient>();
            services.AddSingleton<IPlacesClient, StubPlacesClient>();
        });

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _db.StartAsync();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-tests-signing-key-0123456789-abcdefghij");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
        Environment.SetEnvironmentVariable("RateLimiting__LoginPermitLimit", "1000"); // don't 429 the suite
        Environment.SetEnvironmentVariable("RateLimiting__RefreshPermitLimit", "1000");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public bool CanDeliver => true;
    public IEnumerable<EmailMessage> Sent => _sent;

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}

/// <summary>Knows exactly one place: Veldstraat 1, 9000 Gent. Everything else doesn't match.</summary>
public sealed class StubPlacesClient : IPlacesClient
{
    public const string PlaceId = "ChIJstub-ghent-veldstraat";
    public const double Lat = 51.0543, Lon = 3.7174;

    private static readonly PlaceAddress Ghent = new(PlaceId, "Veldstraat 1, 9000 Gent, België",
        "België", "BE", "Gent", "9000", "Veldstraat", "1", Lat, Lon);

    public Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string text, string? sessionToken, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<PlaceSuggestion>>(
            [new(PlaceId, Ghent.FormattedAddress, "Veldstraat 1", "9000 Gent, België")]);

    public Task<PlaceAddress?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct)
        => Task.FromResult(placeId == PlaceId ? Ghent : null);

    public Task<PlaceAddress?> FindAddressAsync(AddressQuery query, CancellationToken ct)
        => Task.FromResult(query.Street.Equals("Veldstraat", StringComparison.OrdinalIgnoreCase) ? Ghent : null);
}
