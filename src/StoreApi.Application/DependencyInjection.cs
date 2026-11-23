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

/// <summary>Registers catalog (category and product) services.</summary>
public static class CatalogDependencyInjection
{
    /// <summary>Adds category and product services to the container.</summary>
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}

/// <summary>Registers cart services.</summary>
public static class CartDependencyInjection
{
    /// <summary>Adds cart services to the container.</summary>
    public static IServiceCollection AddCart(this IServiceCollection services)
    {
        services.AddScoped<ICartService, CartService>();

        return services;
    }
}

/// <summary>Registers order services.</summary>
public static class OrdersDependencyInjection
{
    /// <summary>Adds order services to the container.</summary>
    public static IServiceCollection AddOrders(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
