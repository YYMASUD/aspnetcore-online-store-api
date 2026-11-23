using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreApi.Domain.Entities;

namespace StoreApi.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private static readonly DateTime SeedDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Stock).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // The RowVersion column type/generation is chosen per provider in StoreDbContext.OnModelCreating.
        builder.HasData(
            new Product
            {
                Id = 1,
                Name = "Wireless Noise-Cancelling Headphones",
                Description = "Over-ear Bluetooth headphones with 30-hour battery life and active noise cancellation.",
                Price = 249.99m,
                Stock = 40,
                ImageUrl = "https://images.example.com/products/headphones.jpg",
                CategoryId = 1,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new Product
            {
                Id = 2,
                Name = "Mechanical Keyboard",
                Description = "Tenkeyless mechanical keyboard with hot-swappable switches and RGB backlighting.",
                Price = 119.50m,
                Stock = 75,
                ImageUrl = "https://images.example.com/products/keyboard.jpg",
                CategoryId = 1,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new Product
            {
                Id = 3,
                Name = "Clean Architecture in .NET",
                Description = "A practical guide to structuring maintainable ASP.NET Core applications.",
                Price = 42.00m,
                Stock = 120,
                ImageUrl = "https://images.example.com/products/clean-architecture.jpg",
                CategoryId = 2,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new Product
            {
                Id = 4,
                Name = "Domain-Driven Design Distilled",
                Description = "A concise introduction to the core ideas of domain-driven design.",
                Price = 35.75m,
                Stock = 90,
                ImageUrl = "https://images.example.com/products/ddd-distilled.jpg",
                CategoryId = 2,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new Product
            {
                Id = 5,
                Name = "Merino Wool Crew Sweater",
                Description = "Lightweight breathable merino wool sweater, suitable for all seasons.",
                Price = 89.99m,
                Stock = 60,
                ImageUrl = "https://images.example.com/products/sweater.jpg",
                CategoryId = 3,
                IsActive = true,
                CreatedAt = SeedDate
            },
            new Product
            {
                Id = 6,
                Name = "Waterproof Trail Jacket",
                Description = "Packable waterproof shell with taped seams and adjustable hood.",
                Price = 159.00m,
                Stock = 25,
                ImageUrl = "https://images.example.com/products/trail-jacket.jpg",
                CategoryId = 3,
                IsActive = true,
                CreatedAt = SeedDate
            });
    }
}
