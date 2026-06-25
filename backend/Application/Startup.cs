using Application.Auth;
using Application.Invites;
using Application.Solicitations;
using Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SolicitationService>();
        services.AddScoped<InviteService>();
        services.AddScoped<RegistrationService>();
        services.AddScoped<UserManagementService>();
        
        return services;
    }
}