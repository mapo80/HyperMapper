namespace HyperMapper.Examples.Runtime.Models;

/// <summary>
/// DTO for Customer - demonstrates computed properties and flattening
/// </summary>
public class CustomerDto
{
    public int Id { get; set; }

    /// <summary>
    /// Computed from FirstName + LastName
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Computed from BirthDate
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Nested object mapping
    /// </summary>
    public AddressDto? Address { get; set; }

    /// <summary>
    /// Collection of order DTOs
    /// </summary>
    public List<OrderDto> Orders { get; set; } = new();

    /// <summary>
    /// Computed property - total order count
    /// </summary>
    public int OrderCount { get; set; }
}
