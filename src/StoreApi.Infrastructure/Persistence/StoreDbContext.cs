using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Domain.Entities;

namespace StoreApi.Infrastructure.Persistence;

/// <summary>EF Core unit of work for the store database.</summary>
public class StoreDbContext(DbContextOptions<StoreDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    private bool UsesSqlServerRowVersion => Database.IsSqlServer();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreDbContext).Assembly);

        var product = modelBuilder.Entity<Product>().Property(x => x.RowVersion);

        // SQL Server: a true rowversion column maintained by the database.
        // Other providers (SQLite in tests): an application-maintained concurrency token,
        // so the optimistic-concurrency code path is exercised identically.
        if (UsesSqlServerRowVersion)
        {
            product.IsRowVersion();
        }
        else
        {
            product.IsConcurrencyToken();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        BumpConcurrencyTokens();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        BumpConcurrencyTokens();
        return base.SaveChanges();
    }

    /// <summary>
    /// SQL Server generates rowversion values itself; on other providers we mint a new
    /// token whenever a product is inserted or updated so stale writes are detected.
    /// </summary>
    private void BumpConcurrencyTokens()
    {
        if (UsesSqlServerRowVersion)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
