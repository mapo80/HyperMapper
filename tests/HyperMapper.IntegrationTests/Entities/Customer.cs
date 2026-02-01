namespace HyperMapper.IntegrationTests.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public Address? Address { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum CustomerType
{
    Regular = 0,
    Premium = 1,
    Vip = 2
}
