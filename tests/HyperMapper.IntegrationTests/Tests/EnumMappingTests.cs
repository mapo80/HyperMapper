using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Entities;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class EnumMappingTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_customer_type_enum_to_string()
    {
        // Arrange
        var regularCustomer = Context.Customers.First(c => c.Type == CustomerType.Regular);
        var premiumCustomer = Context.Customers.First(c => c.Type == CustomerType.Premium);
        var vipCustomer = Context.Customers.First(c => c.Type == CustomerType.Vip);

        // Act
        var regularDto = Mapper.Map<CustomerDto>(regularCustomer);
        var premiumDto = Mapper.Map<CustomerDto>(premiumCustomer);
        var vipDto = Mapper.Map<CustomerDto>(vipCustomer);

        // Assert
        Assert.Equal("Regular", regularDto.CustomerType);
        Assert.Equal("Premium", premiumDto.CustomerType);
        Assert.Equal("Vip", vipDto.CustomerType);
    }

    [Fact]
    public void Should_map_order_status_enum_to_string()
    {
        // Arrange
        var deliveredOrder = Context.Orders
            .Include(o => o.Customer)
            .First(o => o.Status == OrderStatus.Delivered);
        var processingOrder = Context.Orders
            .Include(o => o.Customer)
            .First(o => o.Status == OrderStatus.Processing);
        var cancelledOrder = Context.Orders
            .Include(o => o.Customer)
            .First(o => o.Status == OrderStatus.Cancelled);

        // Act
        var deliveredDto = Mapper.Map<OrderDto>(deliveredOrder);
        var processingDto = Mapper.Map<OrderDto>(processingOrder);
        var cancelledDto = Mapper.Map<OrderDto>(cancelledOrder);

        // Assert
        Assert.Equal("Delivered", deliveredDto.Status);
        Assert.Equal("Processing", processingDto.Status);
        Assert.Equal("Cancelled", cancelledDto.Status);
    }

    [Fact]
    public void Should_map_product_category_enum_to_string()
    {
        // Arrange
        var electronicsProduct = Context.Products.First(p => p.Category == ProductCategory.Electronics);
        var clothingProduct = Context.Products.First(p => p.Category == ProductCategory.Clothing);
        var booksProduct = Context.Products.First(p => p.Category == ProductCategory.Books);

        // Act
        var electronicsDto = Mapper.Map<ProductDto>(electronicsProduct);
        var clothingDto = Mapper.Map<ProductDto>(clothingProduct);
        var booksDto = Mapper.Map<ProductDto>(booksProduct);

        // Assert
        Assert.Equal("Electronics", electronicsDto.Category);
        Assert.Equal("Clothing", clothingDto.Category);
        Assert.Equal("Books", booksDto.Category);
    }

    [Fact]
    public void Should_map_all_enum_values_in_collection()
    {
        // Arrange
        var products = Context.Products.ToList();

        // Act
        var dtos = Mapper.Map<List<ProductDto>>(products);

        // Assert
        Assert.All(dtos, dto =>
        {
            Assert.NotEmpty(dto.Category);
            Assert.True(Enum.TryParse<ProductCategory>(dto.Category, out _));
        });
    }

    [Fact]
    public void Should_map_default_enum_value()
    {
        // Arrange
        var newCustomer = new Customer
        {
            Id = 999,
            Name = "New Customer",
            Email = "new@example.com"
            // Type defaults to CustomerType.Regular (0)
        };

        // Act
        var dto = Mapper.Map<CustomerDto>(newCustomer);

        // Assert
        Assert.Equal("Regular", dto.CustomerType);
    }
}
