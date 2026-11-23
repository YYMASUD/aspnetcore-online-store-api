namespace StoreApi.Domain.Entities;

/// <summary>A product category. Names are unique.</summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
