using Application.Solicitations;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SolicitationService>();
        
        return services;
    }
}