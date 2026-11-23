using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StoreApi.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="StoreDbContext"/> for the <c>dotnet ef</c> tooling so migrations
/// can be generated without booting the API host.
/// </summary>
public sealed class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    private const string FallbackConnectionString =
        "Server=localhost,1433;Database=StoreApiDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

    public StoreDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new StoreDbContext(options);
    }
}
