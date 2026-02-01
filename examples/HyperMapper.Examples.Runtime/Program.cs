using System.Diagnostics;
using HyperMapper;
using HyperMapper.Examples.Runtime.Models;
using HyperMapper.Examples.Runtime.Profiles;

namespace HyperMapper.Examples.Runtime;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   HyperMapper - Runtime Mode Example                      ║");
        Console.WriteLine("║   100% AutoMapper API Compatible                          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ========================================
        // Step 1: Configure the mapper (Runtime Mode)
        // ========================================
        Console.WriteLine("Step 1: Configuring mapper with Runtime Mode...");
        Console.WriteLine("  - Using MapperConfiguration (AutoMapper-compatible API)");
        Console.WriteLine("  - Loading MappingProfile with CreateMap<>() calls");
        Console.WriteLine();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        // Validate configuration
        config.AssertConfigurationIsValid();

        // Create mapper instance
        var mapper = config.CreateMapper();

        Console.WriteLine("✓ Mapper configured successfully");
        Console.WriteLine();

        // ========================================
        // Example 1: Simple Object Mapping
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 1: Simple Object Mapping");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        var address = new Address
        {
            Street = "123 Main Street",
            City = "New York",
            State = "NY",
            ZipCode = "10001"
        };

        var addressDto = mapper.Map<AddressDto>(address);

        Console.WriteLine($"Source: {address.Street}, {address.City}");
        Console.WriteLine($"Result: AddressDto {{");
        Console.WriteLine($"  Street = \"{addressDto.Street}\",");
        Console.WriteLine($"  City = \"{addressDto.City}\",");
        Console.WriteLine($"  FullAddress = \"{addressDto.FullAddress}\"");
        Console.WriteLine($"}}");
        Console.WriteLine();

        // ========================================
        // Example 2: Computed Properties
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 2: Computed Properties (FullName, Age)");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        var customer = new Customer
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            BirthDate = new DateTime(1985, 6, 15),
            Address = address,
            Orders = new List<Order>()
        };

        var customerDto = mapper.Map<CustomerDto>(customer);

        Console.WriteLine($"Source: FirstName=\"{customer.FirstName}\", LastName=\"{customer.LastName}\"");
        Console.WriteLine($"  BirthDate={customer.BirthDate:yyyy-MM-dd}");
        Console.WriteLine($"Result: CustomerDto {{");
        Console.WriteLine($"  FullName = \"{customerDto.FullName}\" (computed)");
        Console.WriteLine($"  Age = {customerDto.Age} (computed)");
        Console.WriteLine($"  Email = \"{customerDto.Email}\"");
        Console.WriteLine($"}}");
        Console.WriteLine();

        // ========================================
        // Example 3: Nested Object Mapping
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 3: Nested Object Mapping");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        Console.WriteLine($"Source: Customer with Address");
        Console.WriteLine($"  Customer.Address.City = \"{customer.Address?.City}\"");
        Console.WriteLine($"Result:");
        Console.WriteLine($"  CustomerDto.Address.City = \"{customerDto.Address?.City}\"");
        Console.WriteLine($"  CustomerDto.Address.FullAddress = \"{customerDto.Address?.FullAddress}\"");
        Console.WriteLine();

        // ========================================
        // Example 4: Collection Mapping
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 4: Collection Mapping");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        // Add orders to customer
        customer.Orders = new List<Order>
        {
            new Order
            {
                Id = 101,
                OrderNumber = "ORD-2024-001",
                OrderDate = DateTime.Now.AddDays(-10),
                TotalAmount = 299.99m,
                Status = OrderStatus.Delivered,
                CustomerId = customer.Id,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 1, ProductName = "Laptop", Quantity = 1, UnitPrice = 299.99m }
                }
            },
            new Order
            {
                Id = 102,
                OrderNumber = "ORD-2024-002",
                OrderDate = DateTime.Now.AddDays(-3),
                TotalAmount = 49.99m,
                Status = OrderStatus.Shipped,
                CustomerId = customer.Id,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 2, ProductName = "Mouse", Quantity = 2, UnitPrice = 24.99m }
                }
            },
            new Order
            {
                Id = 103,
                OrderNumber = "ORD-2024-003",
                OrderDate = DateTime.Now.AddDays(-1),
                TotalAmount = 899.99m,
                Status = OrderStatus.Processing,
                CustomerId = customer.Id,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 3, ProductName = "Monitor", Quantity = 1, UnitPrice = 899.99m }
                }
            }
        };

        var sw = Stopwatch.StartNew();
        customerDto = mapper.Map<CustomerDto>(customer);
        sw.Stop();

        Console.WriteLine($"Mapped {customer.Orders.Count} orders");
        Console.WriteLine($"Time: {sw.Elapsed.TotalMilliseconds:F3}ms");
        Console.WriteLine($"OrderCount (computed): {customerDto.OrderCount}");
        Console.WriteLine();

        // Display order details
        foreach (var orderDto in customerDto.Orders)
        {
            Console.WriteLine($"  Order {orderDto.OrderNumber}:");
            Console.WriteLine($"    Status: {orderDto.Status} (enum → string)");
            Console.WriteLine($"    ItemCount: {orderDto.ItemCount} (computed)");
            Console.WriteLine($"    Total: ${orderDto.TotalAmount}");
        }
        Console.WriteLine();

        // ========================================
        // Example 5: Enum to String Conversion
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 5: Enum to String Conversion");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        var order = customer.Orders[0];
        var singleOrderDto = mapper.Map<OrderDto>(order);

        Console.WriteLine($"Source: Status = OrderStatus.{order.Status} (enum)");
        Console.WriteLine($"Result: Status = \"{singleOrderDto.Status}\" (string)");
        Console.WriteLine();

        // ========================================
        // Example 6: ReverseMap - Bidirectional Mapping
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 6: ReverseMap - Bidirectional Mapping");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        Console.WriteLine("Forward: Address → AddressDto");
        Console.WriteLine($"  Original: {address.City}, {address.State}");

        var forwardDto = mapper.Map<AddressDto>(address);
        Console.WriteLine($"  DTO: {forwardDto.City}, {forwardDto.State}");

        Console.WriteLine();
        Console.WriteLine("Reverse: AddressDto → Address");
        forwardDto.City = "Boston";
        forwardDto.State = "MA";

        var reversedAddress = mapper.Map<Address>(forwardDto);
        Console.WriteLine($"  Result: {reversedAddress.City}, {reversedAddress.State}");
        Console.WriteLine();

        // ========================================
        // Example 7: Map to Existing Object
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 7: Map to Existing Object (Update)");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        var existingDto = new CustomerDto
        {
            Id = 999,
            FullName = "Old Name",
            Email = "old@example.com"
        };

        Console.WriteLine($"Existing DTO: Id={existingDto.Id}, FullName=\"{existingDto.FullName}\"");

        // Map to existing instance (updates properties)
        mapper.Map(customer, existingDto);

        Console.WriteLine($"After Map: Id={existingDto.Id}, FullName=\"{existingDto.FullName}\"");
        Console.WriteLine("  (Properties updated from source)");
        Console.WriteLine();

        // ========================================
        // Example 8: Performance Measurement
        // ========================================
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine("Example 8: Performance Measurement (Runtime Mode)");
        Console.WriteLine("───────────────────────────────────────────────────────────");

        // Warm-up
        for (int i = 0; i < 100; i++)
        {
            _ = mapper.Map<CustomerDto>(customer);
        }

        // Measure
        const int iterations = 10000;
        sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = mapper.Map<CustomerDto>(customer);
        }
        sw.Stop();

        var avgNs = (sw.Elapsed.TotalMilliseconds * 1_000_000) / iterations;
        Console.WriteLine($"Iterations: {iterations:N0}");
        Console.WriteLine($"Total Time: {sw.Elapsed.TotalMilliseconds:F3}ms");
        Console.WriteLine($"Average: {avgNs:F0}ns per mapping");
        Console.WriteLine($"Throughput: {iterations / sw.Elapsed.TotalSeconds:F0} mappings/sec");
        Console.WriteLine();

        // ========================================
        // Summary
        // ========================================
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Runtime Mode Summary                                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("✓ 100% AutoMapper API compatible");
        Console.WriteLine("✓ Dynamic runtime configuration");
        Console.WriteLine("✓ Supports all mapping scenarios");
        Console.WriteLine("✓ Fast execution plans (~100-200ns)");
        Console.WriteLine("✓ Perfect for prototyping and dynamic types");
        Console.WriteLine();
        Console.WriteLine("Next Steps:");
        Console.WriteLine("  - See CodeGen example for 2-3x better performance");
        Console.WriteLine("  - Check README.md for full documentation");
        Console.WriteLine();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
