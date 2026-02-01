using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 ForPath() feature - deeply nested property path configuration.
/// AutoMapper API compatibility: CreateMap<S, D>().ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.StreetName))
/// </summary>
public class ForPathTests
{
    #region Test Models

    public class FlatSource
    {
        public int Id { get; set; }
        public string? StreetName { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
    }

    public class Location
    {
        public string? Country { get; set; }
        public Address? Address { get; set; }
    }

    public class NestedDestination
    {
        public int Id { get; set; }
        public Address? Address { get; set; }
    }

    public class DeeplyNestedDestination
    {
        public int Id { get; set; }
        public Location? Location { get; set; }
    }

    public class PersonSource
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Age { get; set; }
    }

    public class Contact
    {
        public string? FullName { get; set; }
    }

    public class PersonDestination
    {
        public int Id { get; set; }
        public Contact? Contact { get; set; }
        public int Age { get; set; }
    }

    #endregion

    #region Profiles

    public class SingleLevelForPathProfile : Profile
    {
        public SingleLevelForPathProfile()
        {
            CreateMap<FlatSource, NestedDestination>()
                .ForPath(d => d.Address!.Street, opt => opt.MapFrom(s => s.StreetName))
                .ForPath(d => d.Address!.City, opt => opt.MapFrom(s => s.City));
        }
    }

    public class MultipleLevelForPathProfile : Profile
    {
        public MultipleLevelForPathProfile()
        {
            CreateMap<FlatSource, DeeplyNestedDestination>()
                .ForPath(d => d.Location!.Country, opt => opt.MapFrom(s => s.Country))
                .ForPath(d => d.Location!.Address!.Street, opt => opt.MapFrom(s => s.StreetName))
                .ForPath(d => d.Location!.Address!.City, opt => opt.MapFrom(s => s.City));
        }
    }

    public class ForPathWithIgnoreProfile : Profile
    {
        public ForPathWithIgnoreProfile()
        {
            CreateMap<FlatSource, NestedDestination>()
                .ForPath(d => d.Address!.Street, opt => opt.MapFrom(s => s.StreetName))
                .ForPath(d => d.Address!.City, opt => opt.Ignore());
        }
    }

    public class ForPathWithConditionProfile : Profile
    {
        public ForPathWithConditionProfile()
        {
            CreateMap<FlatSource, NestedDestination>()
                .ForPath(d => d.Address!.Street, opt =>
                {
                    opt.MapFrom(s => s.StreetName);
                    opt.Condition(s => !string.IsNullOrEmpty(s.StreetName));
                });
        }
    }

    public class ForPathWithExpressionProfile : Profile
    {
        public ForPathWithExpressionProfile()
        {
            CreateMap<PersonSource, PersonDestination>()
                .ForPath(d => d.Contact!.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        }
    }

    public class ForPathCombinedWithForMemberProfile : Profile
    {
        public ForPathCombinedWithForMemberProfile()
        {
            CreateMap<FlatSource, NestedDestination>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id * 10))
                .ForPath(d => d.Address!.Street, opt => opt.MapFrom(s => s.StreetName));
        }
    }

    public class ForPathWithExistingObjectProfile : Profile
    {
        public ForPathWithExistingObjectProfile()
        {
            CreateMap<FlatSource, NestedDestination>()
                .ForPath(d => d.Address!.Street, opt => opt.MapFrom(s => s.StreetName));
        }
    }

    public class MultipleForPathSameObjectProfile : Profile
    {
        public MultipleForPathSameObjectProfile()
        {
            CreateMap<FlatSource, NestedDestination>()
                .ForPath(d => d.Address!.Street, opt => opt.MapFrom(s => s.StreetName))
                .ForPath(d => d.Address!.City, opt => opt.MapFrom(s => s.City))
                .ForPath(d => d.Address!.PostalCode, opt => opt.MapFrom(s => s.PostalCode));
        }
    }

    #endregion

    [Fact]
    public void ForPath_SingleLevel_CreatesIntermediateAndMaps()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleLevelForPathProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource
        {
            Id = 1,
            StreetName = "123 Main St",
            City = "New York"
        };

        // Act
        var result = mapper.Map<NestedDestination>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Address);
        Assert.Equal("123 Main St", result.Address.Street);
        Assert.Equal("New York", result.Address.City);
    }

    [Fact]
    public void ForPath_MultipleLevel_CreatesAllIntermediates()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleLevelForPathProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource
        {
            Id = 1,
            StreetName = "456 Oak Ave",
            City = "Los Angeles",
            Country = "USA"
        };

        // Act
        var result = mapper.Map<DeeplyNestedDestination>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Location);
        Assert.Equal("USA", result.Location.Country);
        Assert.NotNull(result.Location.Address);
        Assert.Equal("456 Oak Ave", result.Location.Address.Street);
        Assert.Equal("Los Angeles", result.Location.Address.City);
    }

    [Fact]
    public void ForPath_WithIgnore_SkipsIgnoredPath()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForPathWithIgnoreProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource
        {
            Id = 1,
            StreetName = "123 Main St",
            City = "New York"
        };

        // Act
        var result = mapper.Map<NestedDestination>(source);

        // Assert
        Assert.NotNull(result.Address);
        Assert.Equal("123 Main St", result.Address.Street);
        Assert.Null(result.Address.City); // Ignored
    }

    [Fact]
    public void ForPath_WithCondition_AppliesCondition()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForPathWithConditionProfile>());
        var mapper = config.CreateMapper();

        // Act - Condition passes
        var source1 = new FlatSource { Id = 1, StreetName = "123 Main St" };
        var result1 = mapper.Map<NestedDestination>(source1);
        Assert.NotNull(result1.Address);
        Assert.Equal("123 Main St", result1.Address.Street);

        // Act - Condition fails (empty string)
        var source2 = new FlatSource { Id = 2, StreetName = "" };
        var result2 = mapper.Map<NestedDestination>(source2);
        // Address might be created but Street should be null because condition failed
        Assert.True(result2.Address == null || result2.Address.Street == null);
    }

    [Fact]
    public void ForPath_WithMapFromExpression_UsesExpression()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForPathWithExpressionProfile>());
        var mapper = config.CreateMapper();
        var source = new PersonSource
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30
        };

        // Act
        var result = mapper.Map<PersonDestination>(source);

        // Assert
        Assert.NotNull(result.Contact);
        Assert.Equal("John Doe", result.Contact.FullName);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void ForPath_CombinedWithForMember_BothWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForPathCombinedWithForMemberProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource { Id = 5, StreetName = "789 Pine Rd" };

        // Act
        var result = mapper.Map<NestedDestination>(source);

        // Assert
        Assert.Equal(50, result.Id); // ForMember: Id * 10
        Assert.NotNull(result.Address);
        Assert.Equal("789 Pine Rd", result.Address.Street); // ForPath
    }

    [Fact]
    public void ForPath_WithExistingDestination_PreservesExisting()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForPathWithExistingObjectProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource { Id = 1, StreetName = "New Street" };
        var destination = new NestedDestination
        {
            Id = 99,
            Address = new Address
            {
                Street = "Old Street",
                City = "Old City"
            }
        };

        // Act
        mapper.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.Id); // Updated by convention
        Assert.NotNull(destination.Address);
        Assert.Equal("New Street", destination.Address.Street); // Updated by ForPath
        Assert.Equal("Old City", destination.Address.City); // Preserved (not in ForPath)
    }

    [Fact]
    public void ForPath_MultipleForPath_AllApply()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleForPathSameObjectProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource
        {
            Id = 1,
            StreetName = "100 Broadway",
            City = "Manhattan",
            PostalCode = "10001"
        };

        // Act
        var result = mapper.Map<NestedDestination>(source);

        // Assert
        Assert.NotNull(result.Address);
        Assert.Equal("100 Broadway", result.Address.Street);
        Assert.Equal("Manhattan", result.Address.City);
        Assert.Equal("10001", result.Address.PostalCode);
    }

    [Fact]
    public void ForPath_WithNullSourceValue_SetsNullOnLeaf()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleLevelForPathProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource { Id = 1, StreetName = null, City = "NYC" };

        // Act
        var result = mapper.Map<NestedDestination>(source);

        // Assert
        Assert.NotNull(result.Address);
        Assert.Null(result.Address.Street); // Null from source
        Assert.Equal("NYC", result.Address.City);
    }

    [Fact]
    public void ForPath_WithNullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleLevelForPathProfile>());
        var mapper = config.CreateMapper();
        FlatSource? nullSource = null;

        // Act
        var result = mapper.Map<NestedDestination>(nullSource!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ForPath_IntermediateAlreadyExists_ReusesIt()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleLevelForPathProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource { Id = 1, StreetName = "Updated St", City = "Updated City" };
        var existingAddress = new Address { Street = "Original St", City = "Original City", PostalCode = "12345" };
        var destination = new NestedDestination { Id = 99, Address = existingAddress };

        // Act
        mapper.Map(source, destination);

        // Assert
        Assert.Same(existingAddress, destination.Address); // Same instance reused
        Assert.Equal("Updated St", destination.Address.Street);
        Assert.Equal("Updated City", destination.Address.City);
        Assert.Equal("12345", destination.Address.PostalCode); // Preserved
    }
}
