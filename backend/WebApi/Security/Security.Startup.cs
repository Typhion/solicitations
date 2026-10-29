using Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Security;

public static class Startup
{
    internal static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();
        
        return services;
    }
}