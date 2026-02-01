namespace HyperMapper.Benchmarks.Models;

/// <summary>
/// Complex source with nullable, enum, collections
/// </summary>
public class ComplexSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComplexStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal Price { get; set; }
    public int? OptionalQuantity { get; set; }
    public List<string> Tags { get; set; } = new();
    public ComplexAddressSource? Address { get; set; }
}

public class ComplexAddressSource
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
}

public enum ComplexStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2,
    Deleted = 3
}

/// <summary>
/// Complex destination matching source structure
/// </summary>
public class ComplexDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComplexStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal Price { get; set; }
    public int? OptionalQuantity { get; set; }
    public List<string> Tags { get; set; } = new();
    public ComplexAddressDestination? Address { get; set; }
}

public class ComplexAddressDestination
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
}
