using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Catalog;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Common.Models;
using StoreApi.Domain.Entities;

namespace StoreApi.Application.Services;

/// <summary>Read/write operations for categories.</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(CategoryCreateDto request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(int id, CategoryCreateDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Category operations. Deletion is blocked while products still reference the category.</summary>
public sealed class CategoryService(IApplicationDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Products.Count(p => p.IsActive)))
            .ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateAsync(
        CategoryCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await db.Categories.AnyAsync(x => x.Name == name, cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id, category.Name, 0);
    }

    public async Task<CategoryDto> UpdateAsync(
        int id,
        CategoryCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var category = await db.Categories.FindAsync([id], cancellationToken)
                       ?? throw new NotFoundException($"Category {id} was not found.");

        var name = request.Name.Trim();

        if (await db.Categories.AnyAsync(x => x.Name == name && x.Id != id, cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        category.Name = name;
        await db.SaveChangesAsync(cancellationToken);

        var count = await db.Products.CountAsync(x => x.CategoryId == id && x.IsActive, cancellationToken);
        return new CategoryDto(category.Id, category.Name, count);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await db.Categories.FindAsync([id], cancellationToken)
                       ?? throw new NotFoundException($"Category {id} was not found.");

        if (await db.Products.AnyAsync(x => x.CategoryId == id, cancellationToken))
        {
            throw new ConflictException(
                $"Category '{category.Name}' cannot be deleted because it still has products.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }
}
