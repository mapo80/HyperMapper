using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class NestedEntityTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_deeply_nested_entity()
    {
        // Arrange - Order -> Items -> Product
        var order = Context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .First(o => o.Items.Count > 0);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.NotEmpty(dto.Items);
        var firstItem = dto.Items.First();
        Assert.NotEmpty(firstItem.ProductName);
        Assert.NotEmpty(firstItem.ProductSku);
        Assert.True(firstItem.UnitPrice > 0);
    }

    [Fact]
    public void Should_calculate_line_total_from_nested_properties()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .First(o => o.Items.Count > 0);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        foreach (var item in dto.Items)
        {
            Assert.Equal(item.Quantity * item.UnitPrice, item.LineTotal);
        }
    }

    [Fact]
    public void Should_flatten_nested_customer_name()
    {
        // Arrange - Order.Customer.Name -> OrderDto.CustomerName
        var order = Context.Orders
            .Include(o => o.Customer)
            .First();

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.Equal(order.Customer.Name, dto.CustomerName);
    }

    [Fact]
    public void Should_flatten_nested_address_to_string()
    {
        // Arrange - Customer.Address.* -> CustomerSummaryDto.FullAddress
        var customer = Context.Customers
            .Include(c => c.Address)
            .Include(c => c.Orders)
            .First(c => c.Address != null);

        // Act
        var dto = Mapper.Map<CustomerSummaryDto>(customer);

        // Assert
        Assert.NotNull(dto.FullAddress);
        Assert.Contains(customer.Address!.Street, dto.FullAddress);
        Assert.Contains(customer.Address.City, dto.FullAddress);
        Assert.Contains(customer.Address.PostalCode, dto.FullAddress);
        Assert.Contains(customer.Address.Country, dto.FullAddress);
    }

    [Fact]
    public void Should_map_multiple_levels_of_nesting()
    {
        // Arrange - Full customer with all nested data
        var customer = Context.Customers
            .Include(c => c.Address)
            .Include(c => c.Orders)
                .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.Product)
            .First(c => c.Orders.Any(o => o.Items.Count > 0));

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.Orders);

        var orderWithItems = dto.Orders.First(o => o.Items.Count > 0);
        Assert.NotEmpty(orderWithItems.Items);
        Assert.All(orderWithItems.Items, item =>
        {
            Assert.NotEmpty(item.ProductName);
            Assert.True(item.LineTotal > 0);
        });
    }
}
