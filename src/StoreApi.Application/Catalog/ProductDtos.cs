namespace StoreApi.Application.Catalog;

/// <summary>A product as returned by the API, including its category name.</summary>
public sealed record ProductDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string? ImageUrl,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>Payload for creating a product.</summary>
public sealed record ProductCreateDto(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string? ImageUrl,
    int CategoryId);

/// <summary>Payload for updating a product. Null fields are left unchanged.</summary>
public sealed record ProductUpdateDto(
    string? Name,
    string? Description,
    decimal? Price,
    int? Stock,
    string? ImageUrl,
    int? CategoryId,
    bool? IsActive);

/// <summary>Search, filter, sort and pagination parameters for the product list.</summary>
public sealed record ProductQueryParams
{
    public const int MaxPageSize = 50;

    public string? Search { get; init; }

    public int? CategoryId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    /// <summary>One of <c>price_asc</c>, <c>price_desc</c>, <c>name</c>, <c>newest</c>.</summary>
    public string? SortBy { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
