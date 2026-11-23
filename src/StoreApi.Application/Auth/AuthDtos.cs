namespace StoreApi.Application.Auth;

/// <summary>Payload for customer self-registration.</summary>
public sealed record RegisterDto(string Username, string Email, string Password);

/// <summary>Payload for email/password sign-in.</summary>
public sealed record LoginDto(string Email, string Password);

/// <summary>Result of a successful registration or login.</summary>
public sealed record AuthResponseDto(string Token, string Username, string Role, DateTime ExpiresAt);
