using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class CalculatedFieldTests : IntegrationTestBase
{
    [Fact]
    public void Should_calculate_line_total()
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
        foreach (var (entity, dtoItem) in order.Items.Zip(dto.Items))
        {
            var expectedLineTotal = entity.Quantity * entity.UnitPrice;
            Assert.Equal(expectedLineTotal, dtoItem.LineTotal);
        }
    }

    [Fact]
    public void Should_count_orders_in_customer_dto()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First(c => c.Orders.Count > 0);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.Equal(customer.Orders.Count, dto.OrderCount);
    }

    [Fact]
    public void Should_count_items_in_order_summary()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Items)
            .First(o => o.Items.Count > 0);

        // Act
        var dto = Mapper.Map<OrderSummaryDto>(order);

        // Assert
        Assert.Equal(order.Items.Count, dto.ItemCount);
    }

    [Fact]
    public void Should_handle_zero_quantity()
    {
        // Arrange - Create order with zero quantity item
        var order = Context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .First();

        // Modify an item to have zero quantity for test
        var modifiedItem = order.Items.First();
        var originalQuantity = modifiedItem.Quantity;
        modifiedItem.Quantity = 0;

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        var itemDto = dto.Items.First();
        Assert.Equal(0, itemDto.LineTotal);

        // Restore
        modifiedItem.Quantity = originalQuantity;
    }

    [Fact]
    public void Should_calculate_for_multiple_items()
    {
        // Arrange
        var order = Context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .First(o => o.Items.Count > 1);

        // Act
        var dto = Mapper.Map<OrderDto>(order);

        // Assert
        Assert.Equal(order.Items.Count, dto.Items.Count);
        Assert.All(dto.Items, item =>
        {
            Assert.Equal(item.Quantity * item.UnitPrice, item.LineTotal);
        });
    }
}
