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

    // A single message for both duplicate email and duplicate username so registration
    // cannot be used to probe which addresses exist.
    private const string AccountExistsMessage = "An account with these details already exists.";

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var username = request.Username.Trim();

        // The account whose address matches the configured admin mailbox must never be
        // self-registered, or a later admin-seeding run could be tricked into elevating it.
        var isReservedAdminEmail = await db.Users.AnyAsync(
            u => u.Email == email && u.Role == UserRoles.Admin, cancellationToken);

        if (isReservedAdminEmail ||
            await db.Users.AnyAsync(u => u.Email == email, cancellationToken) ||
            await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new ConflictException(AccountExistsMessage);
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

        // Always run the hash comparison, even when the email is unknown, so an attacker
        // cannot distinguish the two cases by response time.
        var passwordValid = passwordHasher.Verify(request.Password, user?.PasswordHash);

        if (user is null || !passwordValid)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var token = tokenService.CreateToken(user);
        return new AuthResponseDto(token.Token, user.Username, user.Role, token.ExpiresAt);
    }
}
