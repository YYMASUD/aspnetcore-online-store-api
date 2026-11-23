using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using StoreApi.Application.Auth;
using StoreApi.Application.Catalog;
using StoreApi.Application.Common.Models;

namespace StoreApi.IntegrationTests;

public class CatalogTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CatalogTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_PublicAccess_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        categories.Should().NotBeNull();
        categories!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProducts_PublicAccess_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProducts_WithFilters_ShouldReturnFilteredResults()
    {
        var response = await _client.GetAsync("/api/products?sortBy=price_asc&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        result.Should().NotBeNull();
        result!.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task CreateProduct_WithoutAdminToken_ShouldReturn401()
    {
        var request = new ProductCreateDto(
            Name: "New Product",
            Description: "Description",
            Price: 99.99m,
            Stock: 10,
            ImageUrl: null,
            CategoryId: 1);

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithAdminToken_ShouldReturn201()
    {
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new ProductCreateDto(
            Name: "Admin Product",
            Description: "Created by admin",
            Price: 149.99m,
            Stock: 25,
            ImageUrl: null,
            CategoryId: 1);

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Admin Product");
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var request = new LoginDto("admin@test.com", "Admin#Passw0rd");
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.Token;
    }
}
