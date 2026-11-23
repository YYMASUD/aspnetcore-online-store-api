using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StoreApi.Application.Auth;
using StoreApi.Application.Services;

namespace StoreApi.Application;

/// <summary>Registers application-layer services and validators.</summary>
public static class DependencyInjection
{
    /// <summary>Adds application services, validators and mappings to the container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: false);

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
