using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using StoreApi.Application.Auth;
using StoreApi.Application.Cart;

namespace StoreApi.IntegrationTests;

public class CartTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CartTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCart_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCart_WithCustomerToken_ShouldReturn200()
    {
        var token = await GetCustomerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToCart_ValidProduct_ShouldReturn201()
    {
        var token = await GetCustomerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new AddToCartDto(ProductId: 1, Quantity: 2);
        var response = await _client.PostAsJsonAsync("/api/cart", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<CartItemDto>();
        item.Should().NotBeNull();
        item!.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddToCart_ExceedsStock_ShouldReturn400()
    {
        var token = await GetCustomerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new AddToCartDto(ProductId: 1, Quantity: 99999);
        var response = await _client.PostAsJsonAsync("/api/cart", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddToCart_MergeExistingItem_ShouldIncreaseQuantity()
    {
        var token = await GetCustomerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request1 = new AddToCartDto(ProductId: 2, Quantity: 1);
        await _client.PostAsJsonAsync("/api/cart", request1);

        var request2 = new AddToCartDto(ProductId: 2, Quantity: 2);
        var response = await _client.PostAsJsonAsync("/api/cart", request2);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<CartItemDto>();
        item!.Quantity.Should().Be(3);
    }

    private async Task<string> GetCustomerTokenAsync()
    {
        var unique = Guid.NewGuid().ToString("N");
        var email = $"customer{unique}@test.com";
        var request = new RegisterDto($"customer{unique[..12]}", email, "Password123!");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.Token;
    }
}
