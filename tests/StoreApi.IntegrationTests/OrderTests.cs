using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using StoreApi.Application.Auth;
using StoreApi.Application.Cart;
using StoreApi.Application.Common.Models;
using StoreApi.Application.Orders;
using StoreApi.Domain.Enums;

namespace StoreApi.IntegrationTests;

public class OrderTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HappyPath_RegisterToAdminShipping_ShouldSucceed()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        var add = await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(1, 2));
        add.StatusCode.Should().Be(HttpStatusCode.Created);

        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout",
            new CheckoutDto("221B Baker Street, London"));
        checkout.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await checkout.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().HaveCount(1);
        order.Items[0].Quantity.Should().Be(2);
        order.Items[0].UnitPrice.Should().BeGreaterThan(0);
        order.TotalAmount.Should().Be(order.Items[0].UnitPrice * 2);

        // Cart is emptied by checkout.
        var cart = await _client.GetFromJsonAsync<CartDto>("/api/cart");
        cart!.Items.Should().BeEmpty();

        // Admin progression Pending -> Paid -> Shipped.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        foreach (var status in new[] { OrderStatus.Paid, OrderStatus.Shipped })
        {
            var update = await _client.PutAsJsonAsync(
                $"/api/orders/{order.Id}/status", new UpdateOrderStatusDto(status));
            update.StatusCode.Should().Be(HttpStatusCode.OK);

            var updated = await update.Content.ReadFromJsonAsync<OrderDto>();
            updated!.Status.Should().Be(status);
        }
    }

    [Fact]
    public async Task Checkout_DecreasesProductStock()
    {
        var productBefore = await _client.GetFromJsonAsync<ProductSnapshot>("/api/products/2");

        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(2, 3));
        (await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street")))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var productAfter = await _client.GetFromJsonAsync<ProductSnapshot>("/api/products/2");
        productAfter!.Stock.Should().Be(productBefore!.Stock - 3);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ShouldReturn400()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_InvalidShippingAddress_ShouldReturn400()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);
        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(1, 1));

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_PendingOrder_ShouldRestoreStockAndCancel()
    {
        var productBefore = await _client.GetFromJsonAsync<ProductSnapshot>("/api/products/3");

        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(3, 4));
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street"));
        var order = await checkout.Content.ReadFromJsonAsync<OrderDto>();

        var cancel = await _client.PutAsync($"/api/orders/{order!.Id}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelled = await cancel.Content.ReadFromJsonAsync<OrderDto>();
        cancelled!.Status.Should().Be(OrderStatus.Cancelled);

        var productAfter = await _client.GetFromJsonAsync<ProductSnapshot>("/api/products/3");
        productAfter!.Stock.Should().Be(productBefore!.Stock);
    }

    [Fact]
    public async Task Customer_CannotAccessAnotherUsersOrder()
    {
        var owner = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(4, 1));
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street"));
        var order = await checkout.Content.ReadFromJsonAsync<OrderDto>();

        var intruder = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", intruder.Token);

        var response = await _client.GetAsync($"/api/orders/{order!.Id}");

        // Non-owners receive 404 for other users' orders so ids cannot be probed.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customer_CannotListAllOrders()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_CannotUpdateOrderStatus()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);

        var response = await _client.PutAsJsonAsync("/api/orders/1/status",
            new UpdateOrderStatusDto(OrderStatus.Shipped));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_InvalidTransition_ShouldReturn400()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);
        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(5, 1));
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street"));
        var order = await checkout.Content.ReadFromJsonAsync<OrderDto>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        // Pending cannot jump straight to Delivered.
        var response = await _client.PutAsJsonAsync(
            $"/api/orders/{order!.Id}/status", new UpdateOrderStatusDto(OrderStatus.Delivered));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_InactiveProduct_ShouldReturn400()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);
        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(6, 1));

        // Admin takes the product off sale after it is already in the cart.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
        var deactivate = await _client.PutAsJsonAsync("/api/products/6", new { isActive = false });
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street"));

        checkout.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Restore for other tests.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
        await _client.PutAsJsonAsync("/api/products/6", new { isActive = true });
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOnlyOwnOrders()
    {
        var customer = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customer.Token);
        await _client.PostAsJsonAsync("/api/cart", new AddToCartDto(1, 1));
        await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutDto("1 Test Street"));

        var mine = await _client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders/my");

        mine.Should().NotBeNull();
        mine!.Items.Should().NotBeEmpty();
        mine.Items.Should().OnlyContain(o => o.UserId == customer.UserId);
    }

    private sealed record ProductSnapshot(int Id, int Stock);

    private async Task<(string Token, int UserId)> RegisterCustomerAsync()
    {
        var unique = Guid.NewGuid().ToString("N");
        var request = new RegisterDto($"cust{unique[..10]}", $"cust{unique}@test.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        var token = auth!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await _client.GetFromJsonAsync<UserInfo>("/api/auth/me");

        return (token, me!.UserId);
    }

    private sealed record UserInfo(int UserId, string Username);

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginDto("admin@test.com", "Admin#Passw0rd"));
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.Token;
    }
}
