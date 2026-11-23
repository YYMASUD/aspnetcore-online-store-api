using Microsoft.EntityFrameworkCore;
using Serilog;
using StoreApi.Infrastructure.Identity;
using StoreApi.Infrastructure.Persistence;

namespace StoreApi.Api.Extensions;

/// <summary>Startup-time database preparation: migrations and default data.</summary>
public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations and seeds the default administrator.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILogger<Program>>();

        var context = provider.GetRequiredService<StoreDbContext>();

        var pending = await context.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            logger.LogInformation("Applying {Count} pending migration(s).", pending.Count());
            await context.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("Database schema is up to date.");
        }

        var seeder = provider.GetRequiredService<AdminSeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>Configures Serilog's bootstrap logger before the host is built.</summary>
    public static void ConfigureBootstrapLogger(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }
}
