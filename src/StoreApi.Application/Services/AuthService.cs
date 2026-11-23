using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Auth;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Domain.Constants;
using StoreApi.Domain.Entities;

namespace StoreApi.Application.Services;

/// <summary>Registration and sign-in. Password hashing and token minting are delegated.</summary>
public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    // Deliberately identical for "unknown email" and "wrong password" so the endpoint
    // cannot be used to enumerate registered accounts.
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        if (await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new ConflictException("This username is already taken.");
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRoles.Customer,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateToken(user);
        return new AuthResponseDto(token.Token, user.Username, user.Role, token.ExpiresAt);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var token = tokenService.CreateToken(user);
        return new AuthResponseDto(token.Token, user.Username, user.Role, token.ExpiresAt);
    }
}
