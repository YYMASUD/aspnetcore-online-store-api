using FluentAssertions;
using StoreApi.Application.Auth;
using StoreApi.Application.Cart;
using StoreApi.Application.Catalog;
using StoreApi.Application.Orders;
using StoreApi.Application.Validators;
using StoreApi.Domain.Enums;

namespace StoreApi.UnitTests;

public class ValidatorTests
{
    private readonly RegisterDtoValidator _register = new();
    private readonly LoginDtoValidator _login = new();
    private readonly CategoryCreateDtoValidator _category = new();
    private readonly ProductCreateDtoValidator _productCreate = new();
    private readonly ProductUpdateDtoValidator _productUpdate = new();
    private readonly ProductQueryParamsValidator _productQuery = new();
    private readonly AddToCartDtoValidator _addToCart = new();
    private readonly UpdateCartDtoValidator _updateCart = new();
    private readonly CheckoutDtoValidator _checkout = new();
    private readonly UpdateOrderStatusDtoValidator _orderStatus = new();
    private readonly OrderQueryParamsValidator _orderQuery = new();

    [Theory]
    [InlineData("alice", "alice@example.com", "Passw0rd!23", true)]
    [InlineData("ab", "alice@example.com", "Passw0rd!23", false)]          // username too short
    [InlineData("alice", "not-an-email", "Passw0rd!23", false)]            // bad email
    [InlineData("alice", "alice@example.com", "short", false)]             // too short
    [InlineData("alice", "alice@example.com", "alllowercase1", false)]     // no uppercase
    [InlineData("alice", "alice@example.com", "ALLUPPERCASE1", false)]     // no lowercase
    [InlineData("alice", "alice@example.com", "NoDigitsHere", false)]      // no digit
    [InlineData("bad name!", "alice@example.com", "Passw0rd!23", false)]   // invalid chars
    public void Register_ShouldValidate(string username, string email, string password, bool valid)
    {
        var result = _register.Validate(new RegisterDto(username, email, password));

        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public void Login_EmptyPassword_ShouldFail() =>
        _login.Validate(new LoginDto("a@b.com", "")).IsValid.Should().BeFalse();

    [Fact]
    public void Login_ValidCredentials_ShouldPass() =>
        _login.Validate(new LoginDto("a@b.com", "anything")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("Electronics", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Category_ShouldValidateName(string name, bool valid) =>
        _category.Validate(new CategoryCreateDto(name)).IsValid.Should().Be(valid);

    [Fact]
    public void ProductCreate_ValidPayload_ShouldPass() =>
        _productCreate.Validate(new ProductCreateDto("Keyboard", "A keyboard", 99.99m, 10, null, 1))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0, 10)]      // price must be positive
    [InlineData(-1, 10)]
    [InlineData(10, -1)]     // stock cannot be negative
    public void ProductCreate_InvalidNumbers_ShouldFail(decimal price, int stock) =>
        _productCreate.Validate(new ProductCreateDto("Keyboard", null, price, stock, null, 1))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ProductCreate_ZeroCategory_ShouldFail() =>
        _productCreate.Validate(new ProductCreateDto("Keyboard", null, 10m, 1, null, 0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ProductUpdate_AllNull_ShouldPass() =>
        _productUpdate.Validate(new ProductUpdateDto(null, null, null, null, null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void ProductUpdate_NegativePrice_ShouldFail() =>
        _productUpdate.Validate(new ProductUpdateDto(null, null, -5m, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData("price_asc", true)]
    [InlineData("price_desc", true)]
    [InlineData("name", true)]
    [InlineData("newest", true)]
    [InlineData(null, true)]
    [InlineData("bogus", false)]
    public void ProductQuery_SortBy_ShouldValidate(string? sortBy, bool valid) =>
        _productQuery.Validate(new ProductQueryParams { SortBy = sortBy }).IsValid.Should().Be(valid);

    [Fact]
    public void ProductQuery_MinGreaterThanMax_ShouldFail() =>
        _productQuery.Validate(new ProductQueryParams { MinPrice = 100m, MaxPrice = 10m })
            .IsValid.Should().BeFalse();

    [Fact]
    public void ProductQuery_PageSizeOverLimit_IsClampedByServiceNotRejected() =>
        _productQuery.Validate(new ProductQueryParams { PageSize = 51 }).IsValid.Should().BeTrue();

    [Fact]
    public void ProductQuery_PageZero_IsClampedByServiceNotRejected() =>
        _productQuery.Validate(new ProductQueryParams { Page = 0 }).IsValid.Should().BeTrue();

    [Fact]
    public void AddToCart_ValidPayload_ShouldPass() =>
        _addToCart.Validate(new AddToCartDto(1, 2)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void AddToCart_InvalidPayload_ShouldFail(int productId, int quantity) =>
        _addToCart.Validate(new AddToCartDto(productId, quantity)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(-1, false)]
    public void UpdateCart_ShouldValidate(int quantity, bool valid) =>
        _updateCart.Validate(new UpdateCartDto(quantity)).IsValid.Should().Be(valid);

    [Fact]
    public void Checkout_EmptyAddress_ShouldFail() =>
        _checkout.Validate(new CheckoutDto("")).IsValid.Should().BeFalse();

    [Fact]
    public void Checkout_ValidAddress_ShouldPass() =>
        _checkout.Validate(new CheckoutDto("221B Baker Street")).IsValid.Should().BeTrue();

    [Fact]
    public void OrderStatus_ValidEnum_ShouldPass() =>
        _orderStatus.Validate(new UpdateOrderStatusDto(OrderStatus.Shipped)).IsValid.Should().BeTrue();

    [Fact]
    public void OrderStatus_OutOfRangeEnum_ShouldFail() =>
        _orderStatus.Validate(new UpdateOrderStatusDto((OrderStatus)99)).IsValid.Should().BeFalse();

    [Fact]
    public void OrderQuery_PageSizeOverLimit_IsClampedByServiceNotRejected() =>
        _orderQuery.Validate(new OrderQueryParams { PageSize = 51 }).IsValid.Should().BeTrue();
}
