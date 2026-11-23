using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StoreApi.Domain.Entities;

namespace StoreApi.Application.Common.Interfaces;

/// <summary>
/// The persistence surface consumed by application services. Implemented by the
/// EF Core <c>StoreDbContext</c> in the infrastructure layer.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<CartItem> CartItems { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    /// <summary>Grants access to transactions and raw SQL.</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
