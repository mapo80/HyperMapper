using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Entities;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class NullHandlingTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_null_entity_to_null()
    {
        // Arrange
        Customer? customer = null;

        // Act
        var dto = Mapper.Map<CustomerDto>(customer!);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public void Should_handle_null_string_property()
    {
        // Arrange
        var product = Context.Products.First(p => p.Description == null);

        // Act
        var dto = Mapper.Map<ProductDto>(product);

        // Assert
        Assert.Null(dto.Description);
    }

    [Fact]
    public void Should_handle_null_navigation()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Address)
            .First(c => c.Address == null);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.Null(dto.Address);
    }

    [Fact]
    public void Should_handle_null_notes_in_order()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Customer)
            .First(o => o.Notes == null);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.Null(dto.Notes);
    }

    [Fact]
    public void Should_handle_non_null_notes_in_order()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Customer)
            .First(o => o.Notes != null);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.NotNull(dto.Notes);
        Assert.Equal(order.Notes, dto.Notes);
    }

    [Fact]
    public void Should_map_empty_collection_not_null()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First(c => c.Orders.Count == 0);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotNull(dto.Orders);
        Assert.Empty(dto.Orders);
    }
}
