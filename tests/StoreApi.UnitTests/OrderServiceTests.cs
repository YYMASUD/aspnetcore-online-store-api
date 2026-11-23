using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Orders;
using StoreApi.Application.Services;
using StoreApi.Domain.Enums;
using StoreApi.Domain.Rules;

namespace StoreApi.UnitTests;

public class OrderServiceTests : UnitTestBase
{
    private async Task<(int UserId, int OrderId)> PlaceOrderAsync(int quantity = 2, int stock = 10)
    {
        var (user, product) = SeedProduct(stock: stock, price: 12.50m);
        var cart = new CartService(Db);
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, quantity));

        var orders = new OrderService(Db);
        var order = await orders.CheckoutAsync(user.Id, new CheckoutDto("10 Downing Street"));

        return (user.Id, order.Id);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldCreateOrderWithPriceSnapshot()
    {
        var (user, product) = SeedProduct(stock: 10, price: 12.50m);
        var cart = new CartService(Db);
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, 3));

        var order = await new OrderService(Db).CheckoutAsync(user.Id, new CheckoutDto("10 Downing Street"));

        order.TotalAmount.Should().Be(37.50m);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().ContainSingle();
        order.Items[0].UnitPrice.Should().Be(12.50m);
        Db.OrderItems.Single().UnitPrice.Should().Be(12.50m);
    }

    [Fact]
    public async Task CheckoutAsync_PriceChangeAfterCheckout_ShouldNotAlterSnapshot()
    {
        var (userId, orderId) = await PlaceOrderAsync();

        var product = await Db.Products.FirstAsync();
        product.Price = 99.99m;
        await Db.SaveChangesAsync();

        var order = await Db.OrderItems.AsNoTracking().SingleAsync();
        order.UnitPrice.Should().Be(12.50m);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldDecreaseStock()
    {
        var (user, product) = SeedProduct(stock: 10);
        var cart = new CartService(Db);
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, 4));

        await new OrderService(Db).CheckoutAsync(user.Id, new CheckoutDto("Somewhere 1"));

        (await Db.Products.AsNoTracking().FirstAsync()).Stock.Should().Be(6);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldClearCart()
    {
        var (user, product) = SeedProduct(stock: 10);
        var cart = new CartService(Db);
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, 1));

        await new OrderService(Db).CheckoutAsync(user.Id, new CheckoutDto("Somewhere 1"));

        Db.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckoutAsync_EmptyCart_ShouldThrow()
    {
        var (user, _) = SeedProduct();

        var act = () => new OrderService(Db).CheckoutAsync(user.Id, new CheckoutDto("Somewhere 1"));

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CheckoutAsync_InsufficientStock_ShouldRollBackAndLeaveCartIntact()
    {
        var (user, product) = SeedProduct(stock: 10);
        var cart = new CartService(Db);
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, 5));
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, 4));

        // Another buyer drains stock behind our back.
        product.Stock = 3;
        await Db.SaveChangesAsync();

        var act = () => new OrderService(Db).CheckoutAsync(user.Id, new CheckoutDto("Somewhere 1"));

        await act.Should().ThrowAsync<BadRequestException>();

        // Transaction rolled back: no order, stock unchanged, cart intact.
        Db.Orders.Should().BeEmpty();
        (await Db.Products.AsNoTracking().FirstAsync()).Stock.Should().Be(3);
        Db.CartItems.Should().ContainSingle();
    }

    [Fact]
    public async Task CheckoutAsync_InactiveProduct_ShouldThrow()
    {
        var (user, product) = SeedProduct(stock: 10);
        var cart = new CartService(Db);
        await cart.AddAsync(user.Id, new Application.Cart.AddToCartDto(product.Id, 1));

        product.IsActive = false;
        await Db.SaveChangesAsync();

        var act = () => new OrderService(Db).CheckoutAsync(user.Id, new CheckoutDto("Somewhere 1"));

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*no longer available*");
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    public void OrderStatusTransitions_ShouldMatchSpecification(
        OrderStatus from, OrderStatus to, bool expected) =>
        OrderStatusTransitions.IsAllowed(from, to).Should().Be(expected);

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ShouldThrow()
    {
        var (_, orderId) = await PlaceOrderAsync();

        var act = () => new OrderService(Db)
            .UpdateStatusAsync(orderId, new UpdateOrderStatusDto(OrderStatus.Delivered));

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ShouldPersist()
    {
        var (_, orderId) = await PlaceOrderAsync();

        var updated = await new OrderService(Db)
            .UpdateStatusAsync(orderId, new UpdateOrderStatusDto(OrderStatus.Paid));

        updated.Status.Should().Be(OrderStatus.Paid);
        (await Db.Orders.AsNoTracking().FirstAsync()).Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task CancelAsync_ShouldRestoreStock()
    {
        var (userId, orderId) = await PlaceOrderAsync(quantity: 3, stock: 10);
        (await Db.Products.AsNoTracking().FirstAsync()).Stock.Should().Be(7);

        var cancelled = await new OrderService(Db).CancelAsync(userId, orderId);

        cancelled.Status.Should().Be(OrderStatus.Cancelled);
        (await Db.Products.AsNoTracking().FirstAsync()).Stock.Should().Be(10);
    }

    [Fact]
    public async Task CancelAsync_NonPendingOrder_ShouldThrow()
    {
        var (userId, orderId) = await PlaceOrderAsync();
        await new OrderService(Db).UpdateStatusAsync(orderId, new UpdateOrderStatusDto(OrderStatus.Paid));

        var act = () => new OrderService(Db).CancelAsync(userId, orderId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Only pending orders*");
    }

    [Fact]
    public async Task CancelAsync_OtherUsersOrder_ShouldThrowForbidden()
    {
        var (_, orderId) = await PlaceOrderAsync();
        var (intruder, _) = SeedProduct();

        var act = () => new OrderService(Db).CancelAsync(intruder.Id, orderId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_OtherUsersOrder_ShouldThrowForbiddenUnlessAdmin()
    {
        var (_, orderId) = await PlaceOrderAsync();
        var (other, _) = SeedProduct();
        var service = new OrderService(Db);

        await FluentActions.Awaiting(() => service.GetByIdAsync(other.Id, orderId, isAdmin: false))
            .Should().ThrowAsync<ForbiddenException>();

        var asAdmin = await service.GetByIdAsync(other.Id, orderId, isAdmin: true);
        asAdmin.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetMyOrdersAsync_ShouldReturnOnlyOwnOrders()
    {
        var (userId, _) = await PlaceOrderAsync();
        var (other, otherProduct) = SeedProduct();
        var cart = new CartService(Db);
        await cart.AddAsync(other.Id, new Application.Cart.AddToCartDto(otherProduct.Id, 1));
        await new OrderService(Db).CheckoutAsync(other.Id, new CheckoutDto("Other Street 2"));

        var mine = await new OrderService(Db).GetMyOrdersAsync(userId, new OrderQueryParams());

        mine.Items.Should().ContainSingle();
        mine.Items.Should().OnlyContain(o => o.UserId == userId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByStatus()
    {
        var (_, orderId) = await PlaceOrderAsync();
        var service = new OrderService(Db);
        await service.UpdateStatusAsync(orderId, new UpdateOrderStatusDto(OrderStatus.Paid));

        var pending = await service.GetAllAsync(new OrderQueryParams { Status = OrderStatus.Pending });
        var paid = await service.GetAllAsync(new OrderQueryParams { Status = OrderStatus.Paid });

        pending.TotalCount.Should().Be(0);
        paid.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyOrdersAsync_PageSizeAboveMaximum_ShouldClamp()
    {
        var (userId, _) = await PlaceOrderAsync();

        var result = await new OrderService(Db).GetMyOrdersAsync(
            userId, new OrderQueryParams { PageSize = 5000 });

        result.PageSize.Should().Be(OrderQueryParams.MaxPageSize);
    }
}
