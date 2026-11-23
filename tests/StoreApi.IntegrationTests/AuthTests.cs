using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FluentAssertions;
using StoreApi.Application.Auth;
using StoreApi.Application.Catalog;

namespace StoreApi.IntegrationTests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ShouldReturn201WithToken()
    {
        var request = new RegisterDto(
            Username: "testuser",
            Email: "test@example.com",
            Password: "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("testuser");
        result.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturn409()
    {
        var request1 = new RegisterDto("user1", "duplicate@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        var request2 = new RegisterDto("user2", "duplicate@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WeakPassword_ShouldReturn400()
    {
        var request = new RegisterDto("testuser", "test@example.com", "weak");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturn200WithToken()
    {
        // Register first
        var registerRequest = new RegisterDto("loginuser", "login@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Login
        var loginRequest = new LoginDto("login@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_ShouldReturn401()
    {
        var registerRequest = new RegisterDto("loginuser2", "login2@example.com", "Password123!");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginDto("login2@example.com", "WrongPassword123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_ShouldReturn401()
    {
        var loginRequest = new LoginDto("unknown@example.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> GetCustomerTokenAsync()
    {
        var email = $"customer{Guid.NewGuid():N}@test.com";
        var request = new RegisterDto("customer", email, "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.Token;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var request = new LoginDto("admin@test.com", "Admin#Passw0rd");
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return result!.Token;
    }
}
