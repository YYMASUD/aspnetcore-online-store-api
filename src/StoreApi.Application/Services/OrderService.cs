using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Common.Models;
using StoreApi.Application.Orders;
using StoreApi.Domain.Entities;
using StoreApi.Domain.Enums;

namespace StoreApi.Application.Services;

/// <summary>Order placement, status transitions and cancellation.</summary>
public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(
        int userId,
        CheckoutDto request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderDto>> GetMyOrdersAsync(
        int userId,
        OrderQueryParams query,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetByIdAsync(
        int userId,
        int id,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderDto>> GetAllAsync(
        OrderQueryParams query,
        CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateStatusAsync(
        int id,
        UpdateOrderStatusDto request,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CancelAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default);
}

/// <summary>Order operations with transactional checkout and optimistic-concurrency handling.</summary>
public sealed class OrderService(IApplicationDbContext db) : IOrderService
{
    public async Task<OrderDto> CheckoutAsync(
        int userId,
        CheckoutDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var cartItems = await db.CartItems
                .Include(x => x.Product)
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            if (cartItems.Count == 0)
            {
                throw new BadRequestException("Cannot checkout an empty cart.");
            }

            var orderItems = new List<OrderItem>(cartItems.Count);
            var total = 0m;

            foreach (var item in cartItems)
            {
                var product = await db.Products.FindAsync([item.ProductId], cancellationToken)
                              ?? throw new NotFoundException($"Product {item.ProductId} was not found.");

                if (!product.IsActive)
                {
                    throw new BadRequestException(
                        $"Product '{product.Name}' is no longer available. Remove it from the cart.");
                }

                if (product.Stock < item.Quantity)
                {
                    throw new BadRequestException(
                        $"Insufficient stock for '{product.Name}': requested {item.Quantity}, available {product.Stock}.");
                }

                product.DecreaseStock(item.Quantity);

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });

                total += product.Price * item.Quantity;
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                ShippingAddress = request.ShippingAddress.Trim(),
                Items = orderItems
            };

            db.Orders.Add(order);
            db.CartItems.RemoveRange(cartItems);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Map(order);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException(
                "Stock changed during checkout. Please review your cart and try again.");
        }
    }

    public async Task<PagedResult<OrderDto>> GetMyOrdersAsync(
        int userId,
        OrderQueryParams query,
        CancellationToken cancellationToken = default) =>
        await GetOrdersAsync(query, userId, cancellationToken);

    public async Task<OrderDto> GetByIdAsync(
        int userId,
        int id,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderWithItemsAsync(id, cancellationToken)
                    ?? throw new NotFoundException($"Order {id} was not found.");

        if (!isAdmin && order.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to view this order.");
        }

        return Map(order);
    }

    public async Task<PagedResult<OrderDto>> GetAllAsync(
        OrderQueryParams query,
        CancellationToken cancellationToken = default) =>
        await GetOrdersAsync(query, null, cancellationToken);

    public async Task<OrderDto> UpdateStatusAsync(
        int id,
        UpdateOrderStatusDto request,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderWithItemsAsync(id, cancellationToken)
                    ?? throw new NotFoundException($"Order {id} was not found.");

        if (!order.CanTransitionTo(request.Status))
        {
            throw new BadRequestException(
                $"Cannot change order status from {order.Status} to {request.Status}.");
        }

        order.ChangeStatus(request.Status);
        await db.SaveChangesAsync(cancellationToken);

        return Map(order);
    }

    public async Task<OrderDto> CancelAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderWithItemsAsync(id, cancellationToken)
                    ?? throw new NotFoundException($"Order {id} was not found.");

        if (order.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to cancel this order.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new BadRequestException("Only pending orders can be cancelled by a customer.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var item in order.Items)
            {
                var product = await db.Products.FindAsync([item.ProductId], cancellationToken)
                              ?? throw new NotFoundException($"Product {item.ProductId} was not found.");

                product.RestoreStock(item.Quantity);
            }

            order.Status = OrderStatus.Cancelled;

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Map(order);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException(
                "Stock changed during cancellation. Please try again.");
        }
    }

    private async Task<PagedResult<OrderDto>> GetOrdersAsync(
        OrderQueryParams query,
        int? userId,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, OrderQueryParams.MaxPageSize);

        var orders = db.Orders.AsNoTracking().AsQueryable();

        if (userId.HasValue)
        {
            orders = orders.Where(x => x.UserId == userId.Value);
        }

        if (query.Status.HasValue)
        {
            orders = orders.Where(x => x.Status == query.Status.Value);
        }

        var totalCount = await orders.CountAsync(cancellationToken);

        var items = await orders
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Items)
            .Select(x => new OrderDto(
                x.Id,
                x.UserId,
                x.OrderDate,
                x.TotalAmount,
                x.Status,
                x.ShippingAddress,
                x.Items.Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.Product.Name,
                    i.Quantity,
                    i.UnitPrice,
                    i.UnitPrice * i.Quantity)).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderDto>(items, totalCount, page, pageSize);
    }

    private async Task<Order?> LoadOrderWithItemsAsync(int id, CancellationToken cancellationToken) =>
        await db.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.UserId,
        order.OrderDate,
        order.TotalAmount,
        order.Status,
        order.ShippingAddress,
        order.Items.Select(i => new OrderItemDto(
            i.Id,
            i.ProductId,
            i.Product.Name,
            i.Quantity,
            i.UnitPrice,
            i.UnitPrice * i.Quantity)).ToList());
}
