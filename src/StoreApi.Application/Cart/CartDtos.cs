namespace StoreApi.Application.Cart;

/// <summary>Adds an item to the current user's cart; the same product merges into an existing line.</summary>
public sealed record AddToCartDto(int ProductId, int Quantity);

/// <summary>Changes the quantity of an existing cart line.</summary>
public sealed record UpdateCartDto(int Quantity);

/// <summary>A single cart line with its computed line total.</summary>
public sealed record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

/// <summary>The user's entire cart with its aggregate total.</summary>
public sealed record CartDto(IReadOnlyList<CartItemDto> Items, decimal Total);
