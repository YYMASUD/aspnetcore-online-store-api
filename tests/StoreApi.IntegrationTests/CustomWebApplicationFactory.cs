using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Infrastructure.Persistence;

namespace StoreApi.IntegrationTests;

/// <summary>
/// Boots the real API against an in-memory SQLite database so integration tests exercise
/// controllers, authentication, validation and persistence together without Docker.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestJwtKey = "test-signing-key-must-be-at-least-32-bytes-long-for-hs256";

    // A single open connection keeps the in-memory database alive for the whole fixture.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    /// <summary>
    /// Minimal-hosting reads configuration while <c>Program.cs</c> runs, which is before
    /// <see cref="ConfigureWebHost"/> callbacks are applied. Seeding the values as process
    /// environment variables guarantees they are visible to <c>WebApplication.CreateBuilder</c>.
    /// </summary>
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=unused;Database=unused;");
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Admin__Username", "admin");
        Environment.SetEnvironmentVariable("Admin__Email", "admin@test.com");
        Environment.SetEnvironmentVariable("Admin__Password", "Admin#Passw0rd");
    }

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<StoreDbContext>)
                            || d.ServiceType == typeof(StoreDbContext)
                            || d.ServiceType == typeof(IApplicationDbContext))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<StoreDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<StoreDbContext>());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<StoreDbContext>().Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}
