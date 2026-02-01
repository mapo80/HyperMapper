using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v10.0.0: Unit tests for IncludeMembers() in Source Generator.
/// Tests flattening of nested objects into the destination.
/// </summary>
public class IncludeMembersSourceGeneratorTests
{
    #region Test Types

    public class InnerDetails
    {
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public InnerDetails Details { get; set; } = new();
    }

    public class PersonFlatDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string ZipCode { get; set; } = "";
    }

    public class ContactInfo
    {
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public Address Address { get; set; } = new();
        public ContactInfo Contact { get; set; } = new();
    }

    public class CustomerFlatDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    public class Metadata
    {
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class Document
    {
        public int DocumentId { get; set; }
        public string Title { get; set; } = "";
        public Metadata Meta { get; set; } = new();
    }

    public class DocumentFlatDto
    {
        public int DocumentId { get; set; }
        public string Title { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Test Profiles

    public class SingleIncludeMembersProfile : Profile
    {
        public SingleIncludeMembersProfile()
        {
            CreateMap<Person, PersonFlatDto>()
                .IncludeMembers(s => s.Details);
        }
    }

    public class MultipleIncludeMembersProfile : Profile
    {
        public MultipleIncludeMembersProfile()
        {
            CreateMap<Customer, CustomerFlatDto>()
                .IncludeMembers(s => s.Address, s => s.Contact);
        }
    }

    public class IncludeMembersWithMetadataProfile : Profile
    {
        public IncludeMembersWithMetadataProfile()
        {
            CreateMap<Document, DocumentFlatDto>()
                .IncludeMembers(s => s.Meta);
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void IncludeMembers_SingleMember_FlattensNestedProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            Id = 1,
            Name = "John Doe",
            Details = new InnerDetails
            {
                Email = "john@example.com",
                Phone = "123-456-7890"
            }
        };

        // Act
        var dest = mapper.Map<PersonFlatDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(1, dest.Id);
        Assert.Equal("John Doe", dest.Name);
        Assert.Equal("john@example.com", dest.Email);
        Assert.Equal("123-456-7890", dest.Phone);
    }

    [Fact]
    public void IncludeMembers_MultipleMembers_FlattensAllNestedProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Customer
        {
            CustomerId = 42,
            CustomerName = "Acme Corp",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                ZipCode = "12345"
            },
            Contact = new ContactInfo
            {
                Email = "info@acme.com",
                Phone = "555-1234"
            }
        };

        // Act
        var dest = mapper.Map<CustomerFlatDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.CustomerId);
        Assert.Equal("Acme Corp", dest.CustomerName);
        Assert.Equal("123 Main St", dest.Street);
        Assert.Equal("Springfield", dest.City);
        Assert.Equal("12345", dest.ZipCode);
        Assert.Equal("info@acme.com", dest.Email);
        Assert.Equal("555-1234", dest.Phone);
    }

    [Fact]
    public void IncludeMembers_WithDateTime_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeMembersWithMetadataProfile>());
        var mapper = config.CreateMapper();
        var createdDate = new DateTime(2024, 1, 15, 10, 30, 0);
        var source = new Document
        {
            DocumentId = 100,
            Title = "Important Document",
            Meta = new Metadata
            {
                CreatedBy = "admin",
                CreatedAt = createdDate
            }
        };

        // Act
        var dest = mapper.Map<DocumentFlatDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(100, dest.DocumentId);
        Assert.Equal("Important Document", dest.Title);
        Assert.Equal("admin", dest.CreatedBy);
        Assert.Equal(createdDate, dest.CreatedAt);
    }

    [Fact]
    public void IncludeMembers_NullNestedMember_HandlesSafely()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            Id = 1,
            Name = "Jane Doe",
            Details = null!  // Null nested object
        };

        // Act
        var dest = mapper.Map<PersonFlatDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(1, dest.Id);
        Assert.Equal("Jane Doe", dest.Name);
        // When nested object is null, IncludeMembers skips it and properties keep their initializer values
        Assert.Equal("", dest.Email);  // Keeps initializer value since Details is null
        Assert.Equal("", dest.Phone);  // Keeps initializer value since Details is null
    }

    [Fact]
    public void IncludeMembers_EmptyNestedMember_MapsEmptyValues()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            Id = 2,
            Name = "Bob Smith",
            Details = new InnerDetails()  // Empty (default values)
        };

        // Act
        var dest = mapper.Map<PersonFlatDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(2, dest.Id);
        Assert.Equal("Bob Smith", dest.Name);
        Assert.Equal("", dest.Email);  // Empty string from default
        Assert.Equal("", dest.Phone);  // Empty string from default
    }

    [Fact]
    public void IncludeMembers_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        Person? nullPerson = null;

        // Act
        var dest = mapper.Map<PersonFlatDto>(nullPerson!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void IncludeMembers_DirectPropertyTakesPrecedence()
    {
        // This test verifies that direct properties on source are mapped first,
        // and IncludeMembers only fills in properties not directly available.

        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            Id = 5,
            Name = "Direct Name",  // Direct property
            Details = new InnerDetails
            {
                Email = "nested@example.com",
                Phone = "999-0000"
            }
        };

        // Act
        var dest = mapper.Map<PersonFlatDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(5, dest.Id);  // Direct property
        Assert.Equal("Direct Name", dest.Name);  // Direct property takes precedence
        Assert.Equal("nested@example.com", dest.Email);  // From IncludeMembers
        Assert.Equal("999-0000", dest.Phone);  // From IncludeMembers
    }

    #endregion
}
