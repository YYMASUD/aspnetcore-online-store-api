namespace StoreApi.Application.Catalog;

/// <summary>A category as returned by the API.</summary>
public sealed record CategoryDto(int Id, string Name, int ProductCount);

/// <summary>Payload for creating or renaming a category.</summary>
public sealed record CategoryCreateDto(string Name);
