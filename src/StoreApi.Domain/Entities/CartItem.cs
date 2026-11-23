namespace StoreApi.Domain.Entities;

/// <summary>A line in a user's shopping cart; unique per (UserId, ProductId).</summary>
public class CartItem
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
