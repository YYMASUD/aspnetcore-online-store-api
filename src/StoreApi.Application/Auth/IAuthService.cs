namespace StoreApi.Application.Auth;

/// <summary>Registration, sign-in and role provisioning.</summary>
public interface IAuthService
{
    /// <summary>Registers a new customer and returns an access token.</summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    /// <summary>Signs a user in, returning the same generic error for unknown email and bad password.</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
}
