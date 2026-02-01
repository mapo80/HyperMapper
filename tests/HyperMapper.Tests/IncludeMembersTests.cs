using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 IncludeMembers() feature - flattening properties from nested source objects.
/// AutoMapper API compatibility: CreateMap<S, D>().IncludeMembers(s => s.Inner)
/// </summary>
public class IncludeMembersTests
{
    #region Test Models

    public class CustomerInfo
    {
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Phone { get; set; }
    }

    public class AddressInfo
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public CustomerInfo? Customer { get; set; }
        public AddressInfo? ShippingAddress { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class InnerSource
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class OuterSource
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public InnerSource? Inner { get; set; }
    }

    public class FlatDest
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Name { get; set; }  // From Inner
        public int Value { get; set; }     // From Inner
    }

    public class PersonInfo
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class ContactInfo
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public PersonInfo? Person { get; set; }
        public ContactInfo? Contact { get; set; }
    }

    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class SourceWithConflict
    {
        public int Id { get; set; }
        public string? Name { get; set; }  // Source wins over nested
        public InnerSource? Inner { get; set; }
    }

    #endregion

    #region Profiles

    public class SingleIncludeMembersProfile : Profile
    {
        public SingleIncludeMembersProfile()
        {
            CreateMap<OuterSource, FlatDest>()
                .IncludeMembers(s => s.Inner);
        }
    }

    public class MultipleIncludeMembersProfile : Profile
    {
        public MultipleIncludeMembersProfile()
        {
            CreateMap<Order, OrderDto>()
                .IncludeMembers(s => s.Customer, s => s.ShippingAddress);
        }
    }

    public class EmployeeIncludeMembersProfile : Profile
    {
        public EmployeeIncludeMembersProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                .IncludeMembers(s => s.Person, s => s.Contact);
        }
    }

    public class PropertyConflictProfile : Profile
    {
        public PropertyConflictProfile()
        {
            // Source.Name should win over Inner.Name
            CreateMap<SourceWithConflict, FlatDest>()
                .IncludeMembers(s => s.Inner);
        }
    }

    public class IncludeMembersWithForMemberProfile : Profile
    {
        public IncludeMembersWithForMemberProfile()
        {
            CreateMap<OuterSource, FlatDest>()
                .IncludeMembers(s => s.Inner)
                .ForMember(d => d.Description, opt => opt.MapFrom(s => "Custom: " + s.Description));
        }
    }

    #endregion

    [Fact]
    public void IncludeMembers_SingleMember_FlattensProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new OuterSource
        {
            Id = 1,
            Description = "Test",
            Inner = new InnerSource { Name = "InnerName", Value = 42 }
        };

        // Act
        var result = mapper.Map<FlatDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Description);
        Assert.Equal("InnerName", result.Name);  // From Inner
        Assert.Equal(42, result.Value);          // From Inner
    }

    [Fact]
    public void IncludeMembers_MultipleMembers_FlattensAll()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Order
        {
            Id = 100,
            Amount = 99.99m,
            Customer = new CustomerInfo
            {
                CustomerName = "John Doe",
                CustomerEmail = "john@example.com",
                Phone = "555-1234"
            },
            ShippingAddress = new AddressInfo
            {
                Street = "123 Main St",
                City = "New York",
                Country = "USA"
            }
        };

        // Act
        var result = mapper.Map<OrderDto>(source);

        // Assert
        Assert.Equal(100, result.Id);
        Assert.Equal(99.99m, result.Amount);
        Assert.Equal("John Doe", result.CustomerName);
        Assert.Equal("john@example.com", result.CustomerEmail);
        Assert.Equal("555-1234", result.Phone);
        Assert.Equal("123 Main St", result.Street);
        Assert.Equal("New York", result.City);
        Assert.Equal("USA", result.Country);
    }

    [Fact]
    public void IncludeMembers_WithNullMember_SkipsProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new OuterSource
        {
            Id = 1,
            Description = "Test",
            Inner = null  // Null nested object
        };

        // Act
        var result = mapper.Map<FlatDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Description);
        Assert.Null(result.Name);     // Not mapped because Inner is null
        Assert.Equal(0, result.Value); // Default value
    }

    [Fact]
    public void IncludeMembers_PropertyConflict_SourceWins()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PropertyConflictProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithConflict
        {
            Id = 1,
            Name = "SourceName",  // This should win
            Inner = new InnerSource { Name = "InnerName", Value = 42 }
        };

        // Act
        var result = mapper.Map<FlatDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("SourceName", result.Name);  // Source wins over Inner
        Assert.Equal(42, result.Value);            // From Inner
    }

    [Fact]
    public void IncludeMembers_WithForMember_ForMemberWins()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeMembersWithForMemberProfile>());
        var mapper = config.CreateMapper();
        var source = new OuterSource
        {
            Id = 1,
            Description = "Original",
            Inner = new InnerSource { Name = "InnerName", Value = 42 }
        };

        // Act
        var result = mapper.Map<FlatDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Custom: Original", result.Description);  // ForMember wins
        Assert.Equal("InnerName", result.Name);  // From Inner
        Assert.Equal(42, result.Value);          // From Inner
    }

    [Fact]
    public void IncludeMembers_MultipleNested_AllFlattened()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EmployeeIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Employee
        {
            EmployeeId = 42,
            Person = new PersonInfo { FirstName = "John", LastName = "Doe" },
            Contact = new ContactInfo { Email = "john@example.com", Phone = "555-1234" }
        };

        // Act
        var result = mapper.Map<EmployeeDto>(source);

        // Assert
        Assert.Equal(42, result.EmployeeId);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("555-1234", result.Phone);
    }

    [Fact]
    public void IncludeMembers_PartialNullNested_OnlyMapsNonNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EmployeeIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Employee
        {
            EmployeeId = 42,
            Person = new PersonInfo { FirstName = "John", LastName = "Doe" },
            Contact = null  // Null contact
        };

        // Act
        var result = mapper.Map<EmployeeDto>(source);

        // Assert
        Assert.Equal(42, result.EmployeeId);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Null(result.Email);  // Contact is null
        Assert.Null(result.Phone);  // Contact is null
    }
}
