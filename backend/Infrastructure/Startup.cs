using Application.Common;
using Application.Invites;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddPersistence(config);
        services.AddSecurity(config);
        services.AddNotifications(config);

        return services;
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