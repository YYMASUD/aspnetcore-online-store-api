using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Cart;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Services;
using StoreApi.Domain.Entities;
using StoreApi.Infrastructure.Persistence;

namespace StoreApi.UnitTests;

public abstract class UnitTestBase : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    protected StoreDbContext Db { get; }

    protected UnitTestBase()
    {
        // Pooling is disabled and the database name is unique: Microsoft.Data.Sqlite pools
        // connections by connection string, which would otherwise make every "in memory"
        // database in the run resolve to the same physical database.
        _connection = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source=file:unitdb{Guid.NewGuid():N}?mode=memory&cache=shared;Pooling=False");
        _connection.Open();

        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new StoreDbContext(options);
        Db.Database.EnsureCreated();

        // EF Core seeds categories and products via HasData; unit tests seed their own
        // fixtures and assert exact counts, so start from a clean slate.
        Db.Products.RemoveRange(Db.Products);
        Db.Categories.RemoveRange(Db.Categories);
        Db.SaveChanges();
    }

    /// <summary>Persists a user, category and product, returning the tracked entities.</summary>
    protected (User User, Product Product) SeedProduct(int stock = 10, decimal price = 25.00m, bool isActive = true)
    {
        var user = new User
        {
            Username = $"user{Guid.NewGuid():N}"[..20],
            Email = $"{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            Role = "Customer"
        };

        var category = new Category { Name = $"Category{Guid.NewGuid():N}"[..20] };

        var product = new Product
        {
            Name = $"Product{Guid.NewGuid():N}"[..20],
            Price = price,
            Stock = stock,
            IsActive = isActive,
            Category = category
        };

        Db.Users.Add(user);
        Db.Products.Add(product);
        Db.SaveChanges();

        return (user, product);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
