using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Entities;
using HyperMapper.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

public class BasicEntityMappingTests : IntegrationTestBase
{
    [Fact]
    public void Should_map_simple_entity_to_dto()
    {
        // Arrange
        var product = Context.Products.First(p => p.Id == 1);

        // Act
        var dto = Mapper.Map<ProductDto>(product);

        // Assert
        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(product.Name, dto.Name);
        Assert.Equal(product.Sku, dto.Sku);
        Assert.Equal(product.Price, dto.Price);
        Assert.Equal(product.Description, dto.Description);
        Assert.Equal(product.Stock, dto.Stock);
        Assert.Equal(product.IsAvailable, dto.IsAvailable);
    }

    [Fact]
    public void Should_map_enum_to_string()
    {
        // Arrange
        var product = Context.Products.First(p => p.Category == ProductCategory.Electronics);

        // Act
        var dto = Mapper.Map<ProductDto>(product);

        // Assert
        Assert.Equal("Electronics", dto.Category);
    }

    [Fact]
    public void Should_map_entity_with_navigation_property()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Address)
            .First(c => c.Id == 1);

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.NotNull(dto.Address);
        Assert.Equal(customer.Address!.Street, dto.Address.Street);
        Assert.Equal(customer.Address.City, dto.Address.City);
    }

    [Fact]
    public void Should_map_entity_with_null_navigation()
    {
        // Arrange
        var customer = Context.Customers
            .Include(c => c.Address)
            .First(c => c.Id == 2); // Jane Smith has no address

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.Null(dto.Address);
    }

    [Fact]
    public void Should_preserve_entity_id()
    {
        // Arrange
        var customer = Context.Customers.First();

        // Act
        var dto = Mapper.Map<CustomerDto>(customer);

        // Assert
        Assert.Equal(customer.Id, dto.Id);
    }

    [Fact]
    public void Should_map_collection_of_entities()
    {
        // Arrange
        var products = Context.Products.ToList();

        // Act
        var dtos = Mapper.Map<List<ProductDto>>(products);

        // Assert
        Assert.Equal(products.Count, dtos.Count);
        for (int i = 0; i < products.Count; i++)
        {
            Assert.Equal(products[i].Id, dtos[i].Id);
            Assert.Equal(products[i].Name, dtos[i].Name);
        }
    }

    [Fact]
    public void Should_map_customer_type_enum_correctly()
    {
        // Arrange
        var premiumCustomer = Context.Customers.First(c => c.Type == CustomerType.Premium);
        var vipCustomer = Context.Customers.First(c => c.Type == CustomerType.Vip);

        // Act
        var premiumDto = Mapper.Map<CustomerDto>(premiumCustomer);
        var vipDto = Mapper.Map<CustomerDto>(vipCustomer);

        // Assert
        Assert.Equal("Premium", premiumDto.CustomerType);
        Assert.Equal("Vip", vipDto.CustomerType);
    }

    [Fact]
    public void Should_handle_default_values_in_entity()
    {
        // Arrange
        var newProduct = new Product
        {
            Id = 100,
            Name = "New Product",
            Sku = "NEW-001",
            Price = 10.00m
            // Stock defaults to 0, IsAvailable defaults to true
        };

        // Act
        var dto = Mapper.Map<ProductDto>(newProduct);

        // Assert
        Assert.Equal(0, dto.Stock);
        Assert.True(dto.IsAvailable);
    }
}
