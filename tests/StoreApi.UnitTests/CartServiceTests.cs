using FluentAssertions;
using StoreApi.Application.Cart;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Services;

namespace StoreApi.UnitTests;

public class CartServiceTests : UnitTestBase
{
    [Fact]
    public async Task AddAsync_NewProduct_ShouldCreateLine()
    {
        var (user, product) = SeedProduct(stock: 10);
        var service = new CartService(Db);

        var item = await service.AddAsync(user.Id, new AddToCartDto(product.Id, 3));

        item.Quantity.Should().Be(3);
        item.ProductId.Should().Be(product.Id);
        item.LineTotal.Should().Be(product.Price * 3);
        Db.CartItems.Should().ContainSingle();
    }

    [Fact]
    public async Task AddAsync_SameProductTwice_ShouldMergeQuantities()
    {
        var (user, product) = SeedProduct(stock: 10);
        var service = new CartService(Db);

        await service.AddAsync(user.Id, new AddToCartDto(product.Id, 2));
        var merged = await service.AddAsync(user.Id, new AddToCartDto(product.Id, 3));

        merged.Quantity.Should().Be(5);
        Db.CartItems.Should().ContainSingle();
    }

    [Fact]
    public async Task AddAsync_ExceedingStock_ShouldThrow()
    {
        var (user, product) = SeedProduct(stock: 5);
        var service = new CartService(Db);

        var act = () => service.AddAsync(user.Id, new AddToCartDto(product.Id, 6));

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*available in stock*");
    }

    [Fact]
    public async Task AddAsync_MergedQuantityExceedingStock_ShouldThrow()
    {
        var (user, product) = SeedProduct(stock: 5);
        var service = new CartService(Db);

        await service.AddAsync(user.Id, new AddToCartDto(product.Id, 4));

        var act = () => service.AddAsync(user.Id, new AddToCartDto(product.Id, 2));

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task AddAsync_InactiveProduct_ShouldThrow()
    {
        var (user, product) = SeedProduct(isActive: false);
        var service = new CartService(Db);

        var act = () => service.AddAsync(user.Id, new AddToCartDto(product.Id, 1));

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*no longer available*");
    }

    [Fact]
    public async Task AddAsync_UnknownProduct_ShouldThrowNotFound()
    {
        var (user, _) = SeedProduct();
        var service = new CartService(Db);

        var act = () => service.AddAsync(user.Id, new AddToCartDto(9999, 1));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddAsync_ZeroQuantity_ShouldThrow()
    {
        var (user, product) = SeedProduct();
        var service = new CartService(Db);

        var act = () => service.AddAsync(user.Id, new AddToCartDto(product.Id, 0));

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetAsync_ShouldComputeTotalAcrossLines()
    {
        var (user, first) = SeedProduct(price: 10m);
        var (_, second) = SeedProduct(price: 2.50m);
        var service = new CartService(Db);

        await service.AddAsync(user.Id, new AddToCartDto(first.Id, 2));
        await service.AddAsync(user.Id, new AddToCartDto(second.Id, 4));

        var cart = await service.GetAsync(user.Id);

        cart.Items.Should().HaveCount(2);
        cart.Total.Should().Be(30m);
    }

    [Fact]
    public async Task UpdateQuantityAsync_ExceedingStock_ShouldThrow()
    {
        var (user, product) = SeedProduct(stock: 5);
        var service = new CartService(Db);
        var item = await service.AddAsync(user.Id, new AddToCartDto(product.Id, 1));

        var act = () => service.UpdateQuantityAsync(user.Id, item.Id, new UpdateCartDto(6));

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task UpdateQuantityAsync_Zero_ShouldRemoveLine()
    {
        var (user, product) = SeedProduct();
        var service = new CartService(Db);
        var item = await service.AddAsync(user.Id, new AddToCartDto(product.Id, 2));

        await service.UpdateQuantityAsync(user.Id, item.Id, new UpdateCartDto(0));

        Db.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateQuantityAsync_OtherUsersItem_ShouldThrowNotFound()
    {
        var (owner, product) = SeedProduct();
        var (intruder, _) = SeedProduct();
        var service = new CartService(Db);
        var item = await service.AddAsync(owner.Id, new AddToCartDto(product.Id, 1));

        var act = () => service.UpdateQuantityAsync(intruder.Id, item.Id, new UpdateCartDto(1));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_OwnItem_ShouldRemove()
    {
        var (user, product) = SeedProduct();
        var service = new CartService(Db);
        var item = await service.AddAsync(user.Id, new AddToCartDto(product.Id, 1));

        await service.RemoveAsync(user.Id, item.Id);

        Db.CartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllLinesForUserOnly()
    {
        var (first, productA) = SeedProduct();
        var (second, productB) = SeedProduct();
        var service = new CartService(Db);

        await service.AddAsync(first.Id, new AddToCartDto(productA.Id, 1));
        await service.AddAsync(first.Id, new AddToCartDto(productB.Id, 1));
        await service.AddAsync(second.Id, new AddToCartDto(productA.Id, 1));

        await service.ClearAsync(first.Id);

        Db.CartItems.Should().ContainSingle();
        Db.CartItems.Single().UserId.Should().Be(second.Id);
    }

    [Fact]
    public async Task AddAsync_InactiveProductAlreadyInCart_UpdateShouldThrow()
    {
        var (user, product) = SeedProduct(stock: 5);
        var service = new CartService(Db);
        var item = await service.AddAsync(user.Id, new AddToCartDto(product.Id, 1));

        product.IsActive = false;
        await Db.SaveChangesAsync();

        var act = () => service.UpdateQuantityAsync(user.Id, item.Id, new UpdateCartDto(2));

        await act.Should().ThrowAsync<BadRequestException>();
    }
}
