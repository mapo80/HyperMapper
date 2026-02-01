using HyperMapper.IntegrationTests.Entities;
using HyperMapper.IntegrationTests.Profiles;
using Microsoft.EntityFrameworkCore;

namespace HyperMapper.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IDisposable
{
    protected readonly TestDbContext Context;
    protected readonly IMapper Mapper;
    private readonly string _databaseName;

    protected IntegrationTestBase()
    {
        _databaseName = $"TestDb_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        Context = new TestDbContext(options);
        Context.Database.EnsureCreated();

        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TestMappingProfile>());
        Mapper = config.CreateMapper();

        SeedData();
    }

    protected virtual void SeedData()
    {
        // Default seed data - can be overridden in derived classes
        SeedProducts();
        SeedCustomers();
        Context.SaveChanges();
    }

    private void SeedProducts()
    {
        var products = new List<Product>
        {
            new()
            {
                Id = 1,
                Name = "Laptop",
                Sku = "LAPTOP-001",
                Price = 999.99m,
                Description = "High-performance laptop",
                Stock = 50,
                Category = ProductCategory.Electronics,
                IsAvailable = true
            },
            new()
            {
                Id = 2,
                Name = "T-Shirt",
                Sku = "TSHIRT-001",
                Price = 29.99m,
                Description = "Cotton t-shirt",
                Stock = 100,
                Category = ProductCategory.Clothing,
                IsAvailable = true
            },
            new()
            {
                Id = 3,
                Name = "Programming Book",
                Sku = "BOOK-001",
                Price = 49.99m,
                Description = "Learn to code",
                Stock = 25,
                Category = ProductCategory.Books,
                IsAvailable = true
            },
            new()
            {
                Id = 4,
                Name = "Out of Stock Item",
                Sku = "OOS-001",
                Price = 19.99m,
                Description = null,
                Stock = 0,
                Category = ProductCategory.Other,
                IsAvailable = false
            }
        };

        Context.Products.AddRange(products);
    }

    private void SeedCustomers()
    {
        var customers = new List<Customer>
        {
            new()
            {
                Id = 1,
                Name = "John Doe",
                Email = "john.doe@example.com",
                Type = CustomerType.Premium,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                IsActive = true,
                Address = new Address
                {
                    Id = 1,
                    Street = "123 Main St",
                    City = "New York",
                    PostalCode = "10001",
                    Country = "USA",
                    CustomerId = 1
                },
                Orders = new List<Order>
                {
                    new()
                    {
                        Id = 1,
                        OrderNumber = "ORD-001",
                        OrderDate = DateTime.UtcNow.AddDays(-30),
                        TotalAmount = 1029.98m,
                        Status = OrderStatus.Delivered,
                        CustomerId = 1,
                        Notes = "Express delivery",
                        Items = new List<OrderItem>
                        {
                            new() { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m },
                            new() { Id = 2, OrderId = 1, ProductId = 2, Quantity = 1, UnitPrice = 29.99m }
                        }
                    },
                    new()
                    {
                        Id = 2,
                        OrderNumber = "ORD-002",
                        OrderDate = DateTime.UtcNow.AddDays(-5),
                        TotalAmount = 49.99m,
                        Status = OrderStatus.Processing,
                        CustomerId = 1,
                        Notes = null,
                        Items = new List<OrderItem>
                        {
                            new() { Id = 3, OrderId = 2, ProductId = 3, Quantity = 1, UnitPrice = 49.99m }
                        }
                    }
                }
            },
            new()
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane.smith@example.com",
                Type = CustomerType.Regular,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                IsActive = true,
                Address = null, // No address
                Orders = new List<Order>() // No orders
            },
            new()
            {
                Id = 3,
                Name = "Bob Wilson",
                Email = "bob.wilson@example.com",
                Type = CustomerType.Vip,
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                IsActive = false, // Inactive customer
                Address = new Address
                {
                    Id = 2,
                    Street = "456 Oak Ave",
                    City = "Los Angeles",
                    PostalCode = "90001",
                    Country = "USA",
                    CustomerId = 3
                },
                Orders = new List<Order>
                {
                    new()
                    {
                        Id = 3,
                        OrderNumber = "ORD-003",
                        OrderDate = DateTime.UtcNow.AddMonths(-12),
                        TotalAmount = 59.98m,
                        Status = OrderStatus.Cancelled,
                        CustomerId = 3,
                        Items = new List<OrderItem>
                        {
                            new() { Id = 4, OrderId = 3, ProductId = 2, Quantity = 2, UnitPrice = 29.99m }
                        }
                    }
                }
            }
        };

        Context.Customers.AddRange(customers);
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
