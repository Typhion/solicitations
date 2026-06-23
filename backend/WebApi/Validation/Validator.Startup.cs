using FluentValidation;

namespace WebApi.Validation;

public static class Startup
{
    internal static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        
        return services;
    }
}