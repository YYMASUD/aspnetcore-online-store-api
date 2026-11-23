using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Cart;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Common.Interfaces;

namespace StoreApi.Application.Services;

/// <summary>Shopping-cart operations scoped to the current user.</summary>
public interface ICartService
{
    Task<CartDto> GetAsync(int userId, CancellationToken cancellationToken = default);

    Task<CartItemDto> AddAsync(
        int userId,
        AddToCartDto request,
        CancellationToken cancellationToken = default);

    Task<CartItemDto> UpdateQuantityAsync(
        int userId,
        int itemId,
        UpdateCartDto request,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        int userId,
        int itemId,
        CancellationToken cancellationToken = default);

    Task ClearAsync(int userId, CancellationToken cancellationToken = default);
}

/// <summary>Cart operations. Ownership is enforced per call.</summary>
public sealed class CartService(IApplicationDbContext db) : ICartService
{
    public async Task<CartDto> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await db.CartItems
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new CartItemDto(
                x.Id,
                x.ProductId,
                x.Product.Name,
                x.Product.Price,
                x.Quantity,
                x.Product.Price * x.Quantity))
            .ToListAsync(cancellationToken);

        return new CartDto(items, items.Sum(x => x.LineTotal));
    }

    public async Task<CartItemDto> AddAsync(
        int userId,
        AddToCartDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new BadRequestException("Quantity must be greater than zero.");
        }

        var product = await db.Products
            .AsNoTracking()
            .Where(x => x.Id == request.ProductId)
            .Select(x => new { x.Id, x.Name, x.Price, x.Stock, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} was not found.");

        if (!product.IsActive)
        {
            throw new BadRequestException($"Product '{product.Name}' is no longer available.");
        }

        var existing = await db.CartItems.FirstOrDefaultAsync(
            x => x.UserId == userId && x.ProductId == request.ProductId,
            cancellationToken);

        var targetQuantity = (existing?.Quantity ?? 0) + request.Quantity;

        if (targetQuantity > product.Stock)
        {
            throw new BadRequestException(
                $"Cannot add {request.Quantity} unit(s): only {product.Stock - (existing?.Quantity ?? 0)} available in stock.");
        }

        if (existing is null)
        {
            existing = new Domain.Entities.CartItem
            {
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            db.CartItems.Add(existing);
        }
        else
        {
            existing.Quantity = targetQuantity;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CartItemDto(
            existing.Id,
            product.Id,
            product.Name,
            product.Price,
            existing.Quantity,
            product.Price * existing.Quantity);
    }

    public async Task<CartItemDto> UpdateQuantityAsync(
        int userId,
        int itemId,
        UpdateCartDto request,
        CancellationToken cancellationToken = default)
    {
        var item = await db.CartItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException($"Cart item {itemId} was not found.");

        if (request.Quantity <= 0)
        {
            db.CartItems.Remove(item);
            await db.SaveChangesAsync(cancellationToken);

            return new CartItemDto(item.Id, item.ProductId, item.Product.Name, item.Product.Price, 0, 0m);
        }

        if (!item.Product.IsActive)
        {
            throw new BadRequestException($"Product '{item.Product.Name}' is no longer available.");
        }

        if (request.Quantity > item.Product.Stock)
        {
            throw new BadRequestException(
                $"Cannot set quantity to {request.Quantity}: only {item.Product.Stock} available in stock.");
        }

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync(cancellationToken);

        return new CartItemDto(
            item.Id, item.ProductId, item.Product.Name, item.Product.Price,
            item.Quantity, item.Product.Price * item.Quantity);
    }

    public async Task RemoveAsync(int userId, int itemId, CancellationToken cancellationToken = default)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(
            x => x.Id == itemId && x.UserId == userId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException($"Cart item {itemId} was not found.");
        }

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(int userId, CancellationToken cancellationToken = default)
    {
        var items = await db.CartItems.Where(x => x.UserId == userId).ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync(cancellationToken);
    }
}
