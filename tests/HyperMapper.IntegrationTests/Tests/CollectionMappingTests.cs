using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Entities;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class CollectionMappingTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_ICollection_to_List()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First(c => c.Id == 1);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotNull(dto.Orders);
        Assert.Equal(customer.Orders.Count, dto.Orders.Count);
    }

    [Fact]
    public void Should_map_empty_collection()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First(c => c.Id == 2); // Jane Smith has no orders

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotNull(dto.Orders);
        Assert.Empty(dto.Orders);
    }

    [Fact]
    public void Should_map_nested_collections()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Customer)
            .First(o => o.Id == 1);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.NotNull(dto.Items);
        Assert.Equal(order.Items.Count, dto.Items.Count);
        Assert.All(dto.Items, item =>
        {
            Assert.NotEmpty(item.ProductName);
            Assert.True(item.LineTotal > 0);
        });
    }

    [Fact]
    public void Should_count_collection_items()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First(c => c.Id == 1);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.Equal(customer.Orders.Count, dto.OrderCount);
    }

    [Fact]
    public void Should_map_all_entities_in_large_collection()
    {
        // Arrange - Add more products for this test
        var additionalProducts = Enumerable.Range(100, 100).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Sku = $"SKU-{i}",
            Price = 10.00m + i,
            Category = ProductCategory.Other
        }).ToList();

        Context.Products.AddRange(additionalProducts);
        Context.SaveChanges();

        var products = Context.Products.ToList();

        // Act
        var dtos = Mapper.Map<List<ProductDto>>(products);

        // Assert
        Assert.Equal(products.Count, dtos.Count);
        Assert.True(dtos.Count > 100);
    }

    [Fact]
    public void Should_map_collection_maintaining_order()
    {
        // Arrange
        var orders = Context.Orders
            .OrderBy(o => o.OrderDate)
            .ToList();

        // Act
        var dtos = Mapper.Map<List<OrderSummaryDto>>(orders);

        // Assert
        for (int i = 0; i < orders.Count; i++)
        {
            Assert.Equal(orders[i].Id, dtos[i].Id);
            Assert.Equal(orders[i].OrderNumber, dtos[i].OrderNumber);
        }
    }
}
