using FluentValidation;
using StoreApi.Application.Catalog;

namespace StoreApi.Application.Validators;

public sealed class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Category name cannot be whitespace.");
    }
}

public sealed class ProductCreateDtoValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(9_999_999.99m);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}

public sealed class ProductUpdateDtoValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(9_999_999.99m).When(x => x.Price.HasValue);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => x.Stock.HasValue);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue);
    }
}

public sealed class ProductQueryParamsValidator : AbstractValidator<ProductQueryParams>
{
    private static readonly string[] AllowedSorts = ["price_asc", "price_desc", "name", "newest"];

    public ProductQueryParamsValidator()
    {
        // page and pageSize are intentionally not range-validated: the specification
        // requires pageSize to be *clamped* to 1..50 rather than rejected, and the service
        // performs that clamping.
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("minPrice cannot be greater than maxPrice.")
            .OverridePropertyName("PriceRange");

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value) ||
                           AllowedSorts.Contains(value.Trim().ToLowerInvariant()))
            .WithMessage("sortBy must be one of: price_asc, price_desc, name, newest.");
    }
}
