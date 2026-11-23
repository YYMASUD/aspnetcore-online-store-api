namespace StoreApi.Application.Common.Interfaces;

/// <summary>Resolves the identity of the caller from the current HTTP context.</summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's id, or <c>null</c> when anonymous.</summary>
    int? UserId { get; }

    /// <summary>The authenticated user's username, or <c>null</c> when anonymous.</summary>
    string? Username { get; }

    /// <summary>Whether the caller holds the Admin role.</summary>
    bool IsAdmin { get; }
}
