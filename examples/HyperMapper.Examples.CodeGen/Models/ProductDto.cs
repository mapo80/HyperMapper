namespace HyperMapper.Examples.CodeGen.Models;

/// <summary>
/// DTO for Product - demonstrates CodeGen features
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>
    /// Stock availability status (computed with PreCondition)
    /// Only mapped if IsActive = true
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Category name (flattened from nested object)
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Full product name with category (computed)
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Age in days since creation (computed)
    /// </summary>
    public int AgeInDays { get; set; }

    /// <summary>
    /// Metadata struct mapping
    /// </summary>
    public ProductMetadataDto Metadata { get; set; } = new();
}

/// <summary>
/// DTO for ProductMetadata struct
/// </summary>
public struct ProductMetadataDto
{
    public string Sku { get; set; }
    public double Weight { get; set; }
    public string Manufacturer { get; set; }
}
