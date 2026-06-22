using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class Startup
{
    internal static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SolicitationsDbContext>(options => options.UseNpgsql(config.GetConnectionString("Default")));

        return services;
    }
}