namespace StoreApi.Domain.Entities;

/// <summary>An immutable line on an order; <see cref="UnitPrice"/> is the price paid at checkout.</summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
