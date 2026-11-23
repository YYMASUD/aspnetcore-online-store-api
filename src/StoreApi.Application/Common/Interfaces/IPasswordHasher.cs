namespace StoreApi.Application.Common.Interfaces;

/// <summary>Hashes and verifies passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password. A <c>null</c> hash is treated as an invalid password but still
    /// performs a full hash comparison, so callers can avoid timing-based user enumeration.
    /// </summary>
    bool Verify(string password, string? passwordHash);
}
