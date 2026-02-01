namespace HyperMapper.Examples.Runtime.Models;

/// <summary>
/// Represents a physical address
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Address
/// </summary>
public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Computed property - formatted address string
    /// </summary>
    public string FullAddress { get; set; } = string.Empty;
}
