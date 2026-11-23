using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StoreApi.Application.Catalog;
using StoreApi.Application.Common.Exceptions;
using StoreApi.Application.Services;
using StoreApi.Domain.Entities;

namespace StoreApi.UnitTests;

public class ProductServiceTests : UnitTestBase
{
    private readonly IProductService _service;

    public ProductServiceTests() => _service = new ProductService(Db);

    private int SeedCatalog(int count = 5)
    {
        var category = new Category { Name = "Seed" };
        Db.Categories.Add(category);
        Db.SaveChanges();

        for (var i = 0; i < count; i++)
        {
            Db.Products.Add(new Product
            {
                Name = $"Product {i}",
                Description = $"Description {i}",
                Price = 10m * (i + 1),
                Stock = 5,
                CategoryId = category.Id,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i)
            });
        }

        Db.SaveChanges();
        return category.Id;
    }

    [Fact]
    public async Task GetPagedAsync_ShouldExcludeInactiveProducts()
    {
        SeedCatalog(3);
        var inactive = await Db.Products.FirstAsync();
        inactive.IsActive = false;
        await Db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new ProductQueryParams());

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public async Task GetPagedAsync_SortByPriceAscending_ShouldOrder()
    {
        SeedCatalog(4);

        var result = await _service.GetPagedAsync(new ProductQueryParams { SortBy = "price_asc" });

        result.Items.Select(p => p.Price).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_SortByPriceDescending_ShouldOrder()
    {
        SeedCatalog(4);

        var result = await _service.GetPagedAsync(new ProductQueryParams { SortBy = "price_desc" });

        result.Items.Select(p => p.Price).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_SortByName_ShouldOrder()
    {
        SeedCatalog(4);

        var result = await _service.GetPagedAsync(new ProductQueryParams { SortBy = "name" });

        result.Items.Select(p => p.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_SortByNewest_ShouldOrderByDateDescending()
    {
        SeedCatalog(4);

        var result = await _service.GetPagedAsync(new ProductQueryParams { SortBy = "newest" });

        result.Items.Select(p => p.CreatedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldPaginate()
    {
        SeedCatalog(9);

        var page1 = await _service.GetPagedAsync(new ProductQueryParams { Page = 1, PageSize = 4 });
        var page3 = await _service.GetPagedAsync(new ProductQueryParams { Page = 3, PageSize = 4 });

        page1.Items.Should().HaveCount(4);
        page1.TotalCount.Should().Be(9);
        page1.TotalPages.Should().Be(3);
        page3.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_PageSizeOverFifty_ShouldClamp()
    {
        SeedCatalog(2);

        var result = await _service.GetPagedAsync(new ProductQueryParams { PageSize = 999 });

        result.PageSize.Should().Be(ProductQueryParams.MaxPageSize);
    }

    [Fact]
    public async Task GetPagedAsync_PageBelowOne_ShouldClampToOne()
    {
        SeedCatalog(2);

        var result = await _service.GetPagedAsync(new ProductQueryParams { Page = -5 });

        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_PriceRange_ShouldFilter()
    {
        SeedCatalog(5);

        var result = await _service.GetPagedAsync(new ProductQueryParams { MinPrice = 20m, MaxPrice = 30m });

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(p => p.Price >= 20m && p.Price <= 30m);
    }

    [Fact]
    public async Task GetPagedAsync_CategoryFilter_ShouldFilter()
    {
        var categoryId = SeedCatalog(3);
        var other = new Category { Name = "Other" };
        Db.Categories.Add(other);
        await Db.SaveChangesAsync();
        Db.Products.Add(new Product { Name = "Foreign", Price = 5m, Stock = 1, CategoryId = other.Id });
        await Db.SaveChangesAsync();

        var result = await _service.GetPagedAsync(new ProductQueryParams { CategoryId = categoryId });

        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(p => p.CategoryId == categoryId);
    }

    [Fact]
    public async Task GetPagedAsync_Search_ShouldMatchName()
    {
        SeedCatalog(3);

        var result = await _service.GetPagedAsync(new ProductQueryParams { Search = "Product 1" });

        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldHideProductFromReads()
    {
        var categoryId = SeedCatalog(1);
        var product = await Db.Products.FirstAsync();

        await _service.SoftDeleteAsync(product.Id);

        (await Db.Products.AsNoTracking().SingleAsync()).IsActive.Should().BeFalse();

        var act = () => _service.GetByIdAsync(product.Id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCategoryName()
    {
        var categoryId = SeedCatalog(0);

        var created = await _service.CreateAsync(
            new ProductCreateDto("New", "Desc", 15m, 3, null, categoryId));

        created.CategoryName.Should().Be("Seed");
        created.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_UnknownCategory_ShouldThrowNotFound()
    {
        var act = () => _service.CreateAsync(new ProductCreateDto("New", null, 15m, 3, null, 9999));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_PartialPayload_ShouldOnlyChangeProvidedFields()
    {
        var categoryId = SeedCatalog(1);
        var product = await Db.Products.AsNoTracking().FirstAsync();
        var originalName = product.Name;

        var updated = await _service.UpdateAsync(product.Id, new ProductUpdateDto(
            Name: null, Description: null, Price: 77m, Stock: null,
            ImageUrl: null, CategoryId: null, IsActive: null));

        updated.Price.Should().Be(77m);
        updated.Name.Should().Be(originalName);
    }

    [Fact]
    public async Task UpdateAsync_ReactivateSoftDeletedProduct_ShouldSucceed()
    {
        var categoryId = SeedCatalog(1);
        var product = await Db.Products.FirstAsync();
        await _service.SoftDeleteAsync(product.Id);

        var updated = await _service.UpdateAsync(product.Id, new ProductUpdateDto(
            null, null, null, null, null, null, IsActive: true));

        updated.IsActive.Should().BeTrue();
        (await _service.GetByIdAsync(product.Id)).Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task CategoryService_DeleteWithProducts_ShouldThrowConflict()
    {
        var categoryId = SeedCatalog(1);
        var service = new CategoryService(Db);

        var act = () => service.DeleteAsync(categoryId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CategoryService_DeleteEmptyCategory_ShouldSucceed()
    {
        var category = new Category { Name = "Empty" };
        Db.Categories.Add(category);
        await Db.SaveChangesAsync();

        await new CategoryService(Db).DeleteAsync(category.Id);

        (await Db.Categories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CategoryService_GetAll_ShouldCountActiveProducts()
    {
        var categoryId = SeedCatalog(3);
        var product = await Db.Products.FirstAsync();
        product.IsActive = false;
        await Db.SaveChangesAsync();

        var categories = await new CategoryService(Db).GetAllAsync();

        categories.Single(c => c.Id == categoryId).ProductCount.Should().Be(2);
    }

    [Fact]
    public async Task CategoryService_CreateDuplicateName_ShouldThrowConflict()
    {
        SeedCatalog(0);
        var service = new CategoryService(Db);

        var act = () => service.CreateAsync(new CategoryCreateDto("Seed"));

        await act.Should().ThrowAsync<ConflictException>();
    }
}
