using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Catalog;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Common.Models;
using StoreApi.Domain.Entities;

namespace StoreApi.Application.Services;

/// <summary>Search, filtering, sorting and CRUD for products.</summary>
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(
        ProductQueryParams query,
        CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(ProductCreateDto request, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(
        int id,
        ProductUpdateDto request,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Product operations. Only active products are visible to customers.</summary>
public sealed class ProductService(IApplicationDbContext db) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        ProductQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, ProductQueryParams.MaxPageSize);
        var search = query.Search?.Trim();
        var sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "name" : query.SortBy.Trim().ToLowerInvariant();

        var products = db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(x =>
                x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(x => x.CategoryId == query.CategoryId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            products = products.Where(x => x.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            products = products.Where(x => x.Price <= query.MaxPrice.Value);
        }

        products = sortBy switch
        {
            "price_asc" => products.OrderBy(x => x.Price).ThenBy(x => x.Id),
            "price_desc" => products.OrderByDescending(x => x.Price).ThenBy(x => x.Id),
            "newest" => products.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            _ => products.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };

        var totalCount = await products.CountAsync(cancellationToken);

        var items = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Description,
                x.Price,
                x.Stock,
                x.ImageUrl,
                x.CategoryId,
                x.Category.Name,
                x.IsActive,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, page, pageSize);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new ProductDto(
                x.Id,
                x.Name,
                x.Description,
                x.Price,
                x.Stock,
                x.ImageUrl,
                x.CategoryId,
                x.Category.Name,
                x.IsActive,
                x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException($"Product {id} was not found.");
    }

    public async Task<ProductDto> CreateAsync(
        ProductCreateDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl?.Trim(),
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        var categoryName = await db.Categories
            .AsNoTracking()
            .Where(x => x.Id == product.CategoryId)
            .Select(x => x.Name)
            .FirstAsync(cancellationToken);

        return new ProductDto(
            product.Id, product.Name, product.Description, product.Price, product.Stock,
            product.ImageUrl, product.CategoryId, categoryName, product.IsActive, product.CreatedAt);
    }

    public async Task<ProductDto> UpdateAsync(
        int id,
        ProductUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
                       ?? throw new NotFoundException($"Product {id} was not found.");

        if (request.CategoryId.HasValue)
        {
            await EnsureCategoryExistsAsync(request.CategoryId.Value, cancellationToken);
        }

        if (request.Name is not null) product.Name = request.Name.Trim();
        if (request.Description is not null) product.Description = request.Description.Trim();
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Stock.HasValue) product.Stock = request.Stock.Value;
        if (request.ImageUrl is not null) product.ImageUrl = request.ImageUrl.Trim();
        if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync(cancellationToken);

        var categoryName = await db.Categories
            .AsNoTracking()
            .Where(x => x.Id == product.CategoryId)
            .Select(x => x.Name)
            .FirstAsync(cancellationToken);

        return new ProductDto(
            product.Id, product.Name, product.Description, product.Price, product.Stock,
            product.ImageUrl, product.CategoryId, categoryName, product.IsActive, product.CreatedAt);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
                       ?? throw new NotFoundException($"Product {id} was not found.");

        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await db.Categories.AnyAsync(x => x.Id == categoryId, cancellationToken))
        {
            throw new NotFoundException($"Category {categoryId} was not found.");
        }
    }
}
