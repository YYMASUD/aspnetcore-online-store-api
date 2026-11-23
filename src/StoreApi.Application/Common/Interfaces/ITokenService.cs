using StoreApi.Domain.Entities;

namespace StoreApi.Application.Common.Interfaces;

/// <summary>A signed JWT and its expiry instant (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>Issues signed access tokens for authenticated users.</summary>
public interface ITokenService
{
    AccessToken CreateToken(User user);
}
