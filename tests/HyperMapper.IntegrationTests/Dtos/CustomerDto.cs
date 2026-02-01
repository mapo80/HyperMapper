namespace HyperMapper.IntegrationTests.Dtos;

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
    public AddressDto? Address { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
}

public class CustomerSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullAddress { get; set; }
    public int OrderCount { get; set; }
}
