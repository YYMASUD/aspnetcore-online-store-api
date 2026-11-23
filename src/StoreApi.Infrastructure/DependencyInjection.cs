using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Common.Models;
using StoreApi.Infrastructure.Identity;
using StoreApi.Infrastructure.Persistence;

namespace StoreApi.Infrastructure;

/// <summary>Registers persistence and identity services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds EF Core, repositories and security services.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Set it via user secrets or the ConnectionStrings__DefaultConnection environment variable.");
        }

        // NOTE: retry-on-failure cannot be combined with user-initiated transactions.
        // Checkout and cancellation use explicit transactions for stock atomicity, so the
        // plain non-retrying strategy is used; concurrency is handled via RowVersion.
        services.AddDbContext<StoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<StoreDbContext>());
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<AdminSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));

        return services;
    }
}
