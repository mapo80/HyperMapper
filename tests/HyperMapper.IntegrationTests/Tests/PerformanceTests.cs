using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Entities;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class PerformanceTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_1000_entities_efficiently()
    {
        // Arrange - Add 1000 customers
        var customers = Enumerable.Range(1, 1000)
            .Select(i => new Customer
            {
                Name = $"Customer {i}",
                Email = $"customer{i}@example.com",
                Type = (CustomerType)(i % 3)
            })
            .ToList();

        Context.Customers.AddRange(customers);
        Context.SaveChanges();

        var allCustomers = Context.Customers.ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var dtos = Mapper.Map<List<CustomerDto>>(allCustomers);
        stopwatch.Stop();

        // Assert
        Assert.Equal(1000 + 3, dtos.Count); // 1000 + 3 from seed data
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Mapping 1000+ entities took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public void Should_map_complex_graph_efficiently()
    {
        // Arrange - Create complex order with many items
        var product = Context.Products.First();
        var customer = Context.Customers.First();

        var order = new Order
        {
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Processing,
            Customer = customer,
            Items = Enumerable.Range(1, 100)
                .Select(i => new OrderItem
                {
                    Product = product,
                    Quantity = i,
                    UnitPrice = product.Price
                })
                .ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        Context.Orders.Add(order);
        Context.SaveChanges();

        var loadedOrder = Context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .First(o => o.Items.Count == 100);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var dto = Mapper.Map<OrderDto>(loadedOrder);
        stopwatch.Stop();

        // Assert
        Assert.Equal(100, dto.Items.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Mapping complex graph took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Should_map_multiple_times_consistently()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Orders)
            .First();

        // Warmup - first few iterations may be slow due to JIT
        for (int i = 0; i < 10; i++)
        {
            Mapper.Map<CustomerDto>(customer);
        }

        // Act - Map same entity multiple times after warmup
        var times = new List<long>();
        for (int i = 0; i < 100; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var dto = Mapper.Map<CustomerDto>(customer);
            stopwatch.Stop();
            times.Add(stopwatch.ElapsedTicks);
        }

        // Assert - Mapping should be consistent (no significant outliers after warmup)
        var average = times.Average();
        var max = times.Max();

        // Max should not be more than 100x the average (very generous for GC pauses etc.)
        Assert.True(max < average * 100,
            $"Inconsistent performance: avg={average}, max={max}");
    }

    [Fact]
    public async Task Should_handle_concurrent_mapping()
    {
        // Arrange
        var customers = Context.Customers
            .Include(c => c.Orders)
            .Include(c => c.Address)
            .ToList();

        // Act - Map concurrently from multiple threads
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                var results = new List<CustomerDto>();
                for (int i = 0; i < 100; i++)
                {
                    foreach (var customer in customers)
                    {
                        results.Add(Mapper.Map<CustomerDto>(customer));
                    }
                }
                return results.Count;
            }))
            .ToArray();

        // Assert - All tasks should complete without exception
        var allResults = await Task.WhenAll(tasks);
        Assert.All(allResults, count => Assert.True(count > 0));
    }
}
