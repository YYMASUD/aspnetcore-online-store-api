namespace StoreApi.Domain.Constants;

/// <summary>Role names used across the domain, API and JWT claims.</summary>
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static readonly string[] All = [Admin, Customer];
}
