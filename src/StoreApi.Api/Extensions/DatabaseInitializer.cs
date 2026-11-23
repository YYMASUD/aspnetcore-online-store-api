using Microsoft.EntityFrameworkCore;
using StoreApi.Infrastructure.Identity;
using StoreApi.Infrastructure.Persistence;

namespace StoreApi.Api.Extensions;

/// <summary>Startup-time database preparation: migrations and default data.</summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations on SQL Server (and creates the schema directly on other
    /// providers such as the SQLite database used by integration tests), then seeds the
    /// default administrator from configuration.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILogger<Program>>();
        var context = provider.GetRequiredService<StoreDbContext>();

        if (context.Database.IsSqlServer())
        {
            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending migration(s).", pending.Count);
                await context.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("Database schema is up to date.");
            }
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        var seeder = provider.GetRequiredService<AdminSeeder>();
        await seeder.SeedAsync();
    }
}
