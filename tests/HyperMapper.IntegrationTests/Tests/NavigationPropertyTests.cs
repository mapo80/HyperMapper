using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class NavigationPropertyTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_one_to_one_navigation()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Address)
            .First(c => c.Address != null);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotNull(dto.Address);
        Assert.Equal(customer.Address!.Id, dto.Address.Id);
        Assert.Equal(customer.Address.Street, dto.Address.Street);
        Assert.Equal(customer.Address.City, dto.Address.City);
    }

    [Fact]
    public void Should_map_one_to_many_navigation()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First(c => c.Orders.Count > 0);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotEmpty(dto.Orders);
        Assert.Equal(customer.Orders.Count, dto.Orders.Count);
    }

    [Fact]
    public void Should_map_many_to_one_navigation()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Customer)
            .First();

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.Equal(order.Customer.Name, dto.CustomerName);
    }

    [Fact]
    public void Should_flatten_navigation_property()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Address)
            .First(c => c.Address != null);

        // Act
        var dto = Mapper.Map<CustomerSummaryDto>(customer);

        // Assert
        Assert.NotNull(dto.FullAddress);
        Assert.Contains(customer.Address!.Street, dto.FullAddress);
        Assert.Contains(customer.Address.City, dto.FullAddress);
        Assert.Contains(customer.Address.Country, dto.FullAddress);
    }

    [Fact]
    public void Should_handle_null_flattened_navigation()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Address)
            .First(c => c.Address == null);

        // Act
        var dto = Mapper.Map<CustomerSummaryDto>(customer);

        // Assert
        Assert.Null(dto.FullAddress);
    }

    [Fact]
    public void Should_map_deep_navigation()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .First(o => o.Items.Count > 0);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.NotEmpty(dto.Items);
        Assert.All(dto.Items, item =>
        {
            Assert.NotEmpty(item.ProductName);
            Assert.NotEmpty(item.ProductSku);
        });
    }
}
