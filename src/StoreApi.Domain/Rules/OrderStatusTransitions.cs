namespace StoreApi.Domain.Rules;

using StoreApi.Domain.Enums;

/// <summary>
/// Single source of truth for order lifecycle rules.
/// Pending → Paid → Shipped → Delivered, and Pending/Paid → Cancelled.
/// </summary>
public static class OrderStatusTransitions
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Allowed =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.Cancelled],
            [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
            [OrderStatus.Shipped] = [OrderStatus.Delivered],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = []
        };

    /// <summary>Returns the statuses reachable from <paramref name="current"/>.</summary>
    public static IReadOnlyList<OrderStatus> From(OrderStatus current) =>
        Allowed.TryGetValue(current, out var next) ? next : [];

    /// <summary>Determines whether <paramref name="current"/> may become <paramref name="target"/>.</summary>
    public static bool IsAllowed(OrderStatus current, OrderStatus target) =>
        Allowed.TryGetValue(current, out var next) && Array.IndexOf(next, target) >= 0;
}
