namespace HyperMapper.Examples.Runtime.Models;

/// <summary>
/// DTO for Order - demonstrates enum to string conversion
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Status converted from enum to string
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Computed property - item count
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Collection of order items
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for OrderItem
/// </summary>
public class OrderItemDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Computed property - line total
    /// </summary>
    public decimal LineTotal { get; set; }
}
