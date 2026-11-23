using StoreApi.Application.Common.Interfaces;

namespace StoreApi.Infrastructure.Identity;

/// <summary>BCrypt-based password hashing with a per-hash salt and work factor 11.</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    // Built once at startup so an unknown-email login does exactly the same BCrypt work as
    // a wrong-password login. This is a throwaway hash, not a credential.
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("constant-time-placeholder", WorkFactor);

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string? passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash ?? DummyHash);
}
