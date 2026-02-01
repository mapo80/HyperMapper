namespace HyperMapper.Examples.Runtime.Models;

/// <summary>
/// Represents a customer entity
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public Address? Address { get; set; }
    public List<Order> Orders { get; set; } = new();
}
