using Application.Account;
using Application.Auth;
using Application.Places;
using Application.Invites;
using Application.Meetings;
using Application.Solicitations;
using Application.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SolicitationService>();
        services.AddScoped<MeetingCalendarService>();
        services.AddScoped<PlacesService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<InviteService>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<AccountService>();
        
        return services;
    }
}