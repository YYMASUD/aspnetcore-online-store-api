using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using StoreApi.Application.Common.Models;
using StoreApi.Domain.Constants;
using StoreApi.Domain.Entities;
using StoreApi.Infrastructure.Identity;

namespace StoreApi.UnitTests;

public class TokenServiceTests
{
    private static TokenService CreateService(string? key = null, int expiryMinutes = 60) =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = key ?? "unit-test-signing-key-must-be-at-least-32-bytes",
            ExpiryMinutes = expiryMinutes
        }));

    private static User CreateUser(string role = UserRoles.Customer) => new()
    {
        Id = 42,
        Username = "alice",
        Email = "alice@example.com",
        PasswordHash = "hash",
        Role = role
    };

    [Fact]
    public void CreateToken_ShouldEmitExpectedClaims()
    {
        var result = CreateService().CreateToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
        jwt.Claims.Should().ContainSingle(c => c.Type == "sub" && c.Value == "42");
        jwt.Claims.Should().ContainSingle(c => c.Type == "name" && c.Value == "alice");
        jwt.Claims.Should().ContainSingle(c => c.Type == "role" && c.Value == UserRoles.Customer);
        jwt.Claims.Should().ContainSingle(c => c.Type == "email" && c.Value == "alice@example.com");
    }

    [Fact]
    public void CreateToken_ShouldCarryAdminRole()
    {
        var result = CreateService().CreateToken(CreateUser(UserRoles.Admin));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        jwt.Claims.Should().ContainSingle(c => c.Type == "role" && c.Value == UserRoles.Admin);
    }

    [Fact]
    public void CreateToken_ShouldRespectExpiryConfiguration()
    {
        var before = DateTime.UtcNow;
        var result = CreateService(expiryMinutes: 15).CreateToken(CreateUser());

        result.ExpiresAt.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(10));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.ValidTo.Should().BeCloseTo(result.ExpiresAt, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CreateToken_ShouldGenerateDistinctJtiPerCall()
    {
        var service = CreateService();
        var first = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(CreateUser()).Token);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(CreateUser()).Token);

        first.Claims.Single(c => c.Type == "jti").Value
            .Should().NotBe(second.Claims.Single(c => c.Type == "jti").Value);
    }

    [Fact]
    public void CreateToken_MissingKey_ShouldThrow() =>
        FluentActions.Invoking(() => CreateService(key: " ").CreateToken(CreateUser()))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key*");
}
