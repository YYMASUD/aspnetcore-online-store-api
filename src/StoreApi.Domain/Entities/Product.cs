namespace StoreApi.Domain.Entities;

using StoreApi.Domain.Exceptions;

/// <summary>A sellable product. Deletion is a soft delete via <see cref="IsActive"/>.</summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optimistic concurrency token (a SQL Server <c>rowversion</c> column).</summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>Whether the requested quantity of this product can be purchased right now.</summary>
    public bool CanFulfil(int quantity) => IsActive && quantity > 0 && Stock >= quantity;

    /// <summary>Reserves stock, enforcing the domain invariant that stock never goes negative.</summary>
    /// <exception cref="DomainException">The product is inactive or stock is insufficient.</exception>
    public void DecreaseStock(int quantity)
    {
        if (!CanFulfil(quantity))
        {
            throw new DomainException(
                $"Product '{Name}' cannot fulfil a request for {quantity} unit(s); {Stock} available.");
        }

        Stock -= quantity;
    }

    /// <summary>Returns previously reserved units to stock (used when an order is cancelled).</summary>
    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Restored quantity must be greater than zero.");
        }

        Stock += quantity;
    }
}
