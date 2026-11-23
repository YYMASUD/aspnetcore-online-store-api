using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Common.Models;
using StoreApi.Domain.Constants;
using StoreApi.Domain.Entities;

namespace StoreApi.Infrastructure.Identity;

/// <summary>
/// Creates the default administrator on startup when <c>Admin:Email</c> and
/// <c>Admin:Password</c> are configured. Credentials are never hardcoded: without
/// configuration the seeder logs a warning and does nothing.
/// </summary>
public sealed class AdminSeeder(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<AdminOptions> options,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = options.Value;

        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning(
                "Admin seeding skipped: Admin:Email and Admin:Password are not configured. " +
                "Set them via user secrets or environment variables to create the default administrator.");
            return;
        }

        if (admin.Password.Length < 8)
        {
            logger.LogWarning("Admin seeding skipped: Admin:Password must be at least 8 characters long.");
            return;
        }

        var email = admin.Email.Trim();
        var username = string.IsNullOrWhiteSpace(admin.Username) ? "admin" : admin.Username.Trim();

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existing is not null)
        {
            if (existing.Role != UserRoles.Admin)
            {
                existing.Role = UserRoles.Admin;
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Promoted existing account {Email} to the Admin role.", email);
            }

            return;
        }

        db.Users.Add(new User
        {
            Username = await ResolveUniqueUsernameAsync(username, cancellationToken),
            Email = email,
            PasswordHash = passwordHasher.Hash(admin.Password),
            Role = UserRoles.Admin,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Default administrator account {Email} created.", email);
    }

    private async Task<string> ResolveUniqueUsernameAsync(string candidate, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            return candidate;
        }

        var suffix = 1;
        string attempt;
        do
        {
            attempt = $"{candidate}{suffix++}";
        }
        while (await db.Users.AnyAsync(u => u.Username == attempt, cancellationToken));

        return attempt;
    }
}
