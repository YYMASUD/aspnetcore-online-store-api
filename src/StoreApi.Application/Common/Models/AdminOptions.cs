namespace StoreApi.Application.Common.Models;

/// <summary>Configuration for the default admin account created at startup.</summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = "admin";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
