namespace HyperMapper.IntegrationTests.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int Stock { get; set; }
    public ProductCategory Category { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public enum ProductCategory
{
    Electronics = 0,
    Clothing = 1,
    Books = 2,
    Food = 3,
    Other = 4
}
