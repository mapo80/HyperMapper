namespace HyperMapper.Examples.CodeGen.Models;

/// <summary>
/// Product entity
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public ProductMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Product metadata (struct for compile-time demonstration)
/// </summary>
public struct ProductMetadata
{
    public string Sku { get; set; }
    public double Weight { get; set; }
    public string Manufacturer { get; set; }
}
