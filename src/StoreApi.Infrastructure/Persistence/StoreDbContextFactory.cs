using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StoreApi.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="StoreDbContext"/> for the <c>dotnet ef</c> tooling so migrations
/// can be generated without booting the API host. The connection string is read from
/// configuration or environment variables; no credential is hardcoded.
/// </summary>
public sealed class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection (or the " +
                "ConnectionStrings__DefaultConnection environment variable) before running dotnet ef.");
        }

        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new StoreDbContext(options);
    }
}
