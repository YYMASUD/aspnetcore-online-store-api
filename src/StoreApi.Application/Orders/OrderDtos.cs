namespace StoreApi.Application.Orders;

using StoreApi.Domain.Enums;

/// <summary>Payload for creating an order from the current user's cart.</summary>
public sealed record CheckoutDto(string ShippingAddress);

/// <summary>Payload for admin-side status updates.</summary>
public sealed record UpdateOrderStatusDto(OrderStatus Status);

/// <summary>A snapshot of an item at checkout time.</summary>
public sealed record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>An order as returned by the API.</summary>
public sealed record OrderDto(
    int Id,
    int UserId,
    DateTime OrderDate,
    decimal TotalAmount,
    OrderStatus Status,
    string ShippingAddress,
    IReadOnlyList<OrderItemDto> Items);

/// <summary>Filters for the admin-only order list.</summary>
public sealed record OrderQueryParams
{
    public const int MaxPageSize = 50;

    public OrderStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
