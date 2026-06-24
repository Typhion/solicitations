using Application.Common;

namespace WebApi.Security;

public static class Startup
{
    internal static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        
        return services;
    }
}