using Application.Common;
using Application.Places;
using Application.Invites;
using Infrastructure.Places;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddPersistence(config);
        services.AddSecurity(config);
        services.AddNotifications(config);
        services.AddPlaces(config);

        return services;
    }

    private static void AddPlaces(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<GoogleMapsSettings>()
            .Bind(config.GetSection(GoogleMapsSettings.SectionName))
            .Validate(s => s.RefreshAfterDays is > 0 and < 30,
                "GoogleMaps:RefreshAfterDays must be 1-29 (Google allows caching coordinates for 30 days at most).")
            .Validate(s => s.BiasRadiusMeters is > 0 and <= 50_000, "GoogleMaps:BiasRadiusMeters must be 1-50000.")
            .ValidateOnStart();

        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        // Real Google once GoogleMaps:ServerApiKey is set; otherwise a client that verifies nothing.
        services.AddHttpClient<GooglePlacesClient>((sp, http) =>
        {
            var s = sp.GetRequiredService<IOptions<GoogleMapsSettings>>().Value;
            http.BaseAddress = new Uri(s.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
        });
        services.AddSingleton<DisabledPlacesClient>();
        services.AddScoped<IPlacesClient>(sp =>
            sp.GetRequiredService<IOptions<GoogleMapsSettings>>().Value.IsConfigured
                ? sp.GetRequiredService<GooglePlacesClient>()
                : sp.GetRequiredService<DisabledPlacesClient>());

        // Refreshes coordinates (30-day limit) and verifies pre-existing addresses once.
        services.AddHostedService<LocationMaintenanceService>();
    }

    private static void AddNotifications(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AppSettings>()
            .Bind(config.GetSection(AppSettings.SectionName))
            .Validate(s => Uri.TryCreate(s.FrontendBaseUrl, UriKind.Absolute, out _),
                "App:FrontendBaseUrl must be an absolute URL (used for links in emails).")
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(config.GetSection(EmailSettings.SectionName))
            .Validate(s => !s.IsConfigured || !string.IsNullOrWhiteSpace(s.FromAddress),
                "Email:FromAddress is required when Email:Host is set.")
            .ValidateOnStart();

        // Real SMTP once Email:Host is configured; otherwise a sender that delivers nothing.
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<LoggingEmailSender>();
        services.AddScoped<IEmailSender>(sp =>
            sp.GetRequiredService<IOptions<EmailSettings>>().Value.IsConfigured
                ? sp.GetRequiredService<SmtpEmailSender>()
                : sp.GetRequiredService<LoggingEmailSender>());
        services.AddScoped<IInviteNotifier, InviteNotifier>();
    }

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(ct);
    }
}