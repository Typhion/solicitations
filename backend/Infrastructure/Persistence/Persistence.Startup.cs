using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Solicitations;

namespace Infrastructure.Persistence;

public static class Startup
{
    internal static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SolicitationsDbContext>(options => options.UseNpgsql(config.GetConnectionString("Default")));
        services.AddScoped<IDataSeed, Seeds.AdminSeed>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<ISolicitationRepository, SolicitationRepository>();

        return services;
    }
}