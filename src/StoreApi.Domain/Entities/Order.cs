namespace StoreApi.Domain.Entities;

using StoreApi.Domain.Enums;
using StoreApi.Domain.Exceptions;
using StoreApi.Domain.Rules;

/// <summary>An order placed by a customer. Items store a snapshot of the price at purchase time.</summary>
public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string ShippingAddress { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>Whether <paramref name="target"/> is a legal next state for this order.</summary>
    public bool CanTransitionTo(OrderStatus target) => OrderStatusTransitions.IsAllowed(Status, target);

    /// <summary>Applies a validated lifecycle transition.</summary>
    /// <exception cref="DomainException">The transition is not allowed.</exception>
    public void ChangeStatus(OrderStatus target)
    {
        if (!CanTransitionTo(target))
        {
            throw new DomainException(
                $"Cannot change order status from {Status} to {target}.");
        }

        Status = target;
    }

    /// <summary>Customer-side cancellation, only permitted for pending orders.</summary>
    /// <exception cref="DomainException">The order is no longer pending.</exception>
    public void Cancel()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only pending orders can be cancelled by a customer.");
        }

        Status = OrderStatus.Cancelled;
    }
}
