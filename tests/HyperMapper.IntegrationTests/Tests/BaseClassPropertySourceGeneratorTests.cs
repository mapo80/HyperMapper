using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

/// <summary>
/// v12.1.0: Tests for property lookup in base classes.
/// Verifies GetPropertyByNameIncludingBase works correctly when MapFrom references
/// properties defined in parent classes.
/// </summary>
public class BaseClassPropertySourceGeneratorTests
{
    #region Test Models - Simple Inheritance

    public class BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class DerivedEntity : BaseEntity
    {
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class DerivedEntityDto
    {
        public int EntityId { get; set; }
        public string EntityName { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
        public string CreatedAtFormatted { get; set; } = "";
    }

    #endregion

    #region Test Models - Deep Inheritance (3 levels)

    public class Level1Base
    {
        public string Level1Property { get; set; } = "";
    }

    public class Level2Base : Level1Base
    {
        public string Level2Property { get; set; } = "";
    }

    public class Level3Entity : Level2Base
    {
        public string Level3Property { get; set; } = "";
    }

    public class Level3Dto
    {
        public string FromLevel1 { get; set; } = "";
        public string FromLevel2 { get; set; } = "";
        public string FromLevel3 { get; set; } = "";
        public string CombinedLevels { get; set; } = "";
    }

    #endregion

    #region Test Models - Mixed Access Patterns

    public class PersonBase
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class Employee : PersonBase
    {
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
    }

    public class EmployeeDto
    {
        public string FullName { get; set; } = "";
        public string Department { get; set; } = "";
        public string SalaryFormatted { get; set; } = "";
    }

    #endregion

    #region Profiles

    public class BaseClassPropertyProfile : Profile
    {
        public BaseClassPropertyProfile()
        {
            CreateMap<DerivedEntity, DerivedEntityDto>()
                // MapFrom referencing base class properties
                .ForMember(d => d.EntityId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.EntityName, opt => opt.MapFrom(s => s.Name))
                .ForMember(d => d.CreatedAtFormatted, opt => opt.MapFrom(s => s.CreatedAt.ToString("yyyy-MM-dd")));
        }
    }

    public class DeepInheritanceProfile : Profile
    {
        public DeepInheritanceProfile()
        {
            CreateMap<Level3Entity, Level3Dto>()
                // MapFrom referencing properties from different levels
                .ForMember(d => d.FromLevel1, opt => opt.MapFrom(s => s.Level1Property))
                .ForMember(d => d.FromLevel2, opt => opt.MapFrom(s => s.Level2Property))
                .ForMember(d => d.FromLevel3, opt => opt.MapFrom(s => s.Level3Property))
                .ForMember(d => d.CombinedLevels, opt => opt.MapFrom(s =>
                    $"{s.Level1Property}-{s.Level2Property}-{s.Level3Property}"));
        }
    }

    public class MixedAccessProfile : Profile
    {
        public MixedAccessProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                // Combining base and derived properties in expression
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.SalaryFormatted, opt => opt.MapFrom(s => s.Salary.ToString("C2")));
        }
    }

    #endregion

    #region Simple Inheritance Tests

    [Fact]
    public void CodeGen_MapFromBaseClassProperty_ShouldResolveCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BaseClassPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new DerivedEntity
        {
            Id = 42,
            Name = "Test Entity",
            CreatedAt = new DateTime(2024, 1, 15),
            Description = "Test Description",
            IsActive = true
        };

        // Act
        var result = mapper.Map<DerivedEntityDto>(source);

        // Assert
        Assert.Equal(42, result.EntityId);
        Assert.Equal("Test Entity", result.EntityName);
        Assert.Equal("2024-01-15", result.CreatedAtFormatted);
        Assert.Equal("Test Description", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void CodeGen_MultipleBaseClassProperties_ShouldMapAll()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BaseClassPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new DerivedEntity
        {
            Id = 100,
            Name = "Multiple Properties Test",
            CreatedAt = new DateTime(2023, 6, 1),
            Description = "Desc",
            IsActive = false
        };

        // Act
        var result = mapper.Map<DerivedEntityDto>(source);

        // Assert
        Assert.Equal(100, result.EntityId);
        Assert.Equal("Multiple Properties Test", result.EntityName);
        Assert.Equal("2023-06-01", result.CreatedAtFormatted);
    }

    #endregion

    #region Deep Inheritance Tests

    [Fact]
    public void CodeGen_DeepInheritance_ShouldFindPropertyInAncestor()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new Level3Entity
        {
            Level1Property = "Level1Value",
            Level2Property = "Level2Value",
            Level3Property = "Level3Value"
        };

        // Act
        var result = mapper.Map<Level3Dto>(source);

        // Assert
        Assert.Equal("Level1Value", result.FromLevel1);
        Assert.Equal("Level2Value", result.FromLevel2);
        Assert.Equal("Level3Value", result.FromLevel3);
        Assert.Equal("Level1Value-Level2Value-Level3Value", result.CombinedLevels);
    }

    [Fact]
    public void CodeGen_DeepInheritance_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new Level3Entity
        {
            Level1Property = "A",
            Level2Property = "",
            Level3Property = "C"
        };

        // Act
        var result = mapper.Map<Level3Dto>(source);

        // Assert
        Assert.Equal("A", result.FromLevel1);
        Assert.Equal("", result.FromLevel2);
        Assert.Equal("C", result.FromLevel3);
        Assert.Equal("A--C", result.CombinedLevels);
    }

    #endregion

    #region Mixed Access Pattern Tests

    [Fact]
    public void CodeGen_CombineBaseAndDerivedProperties_ShouldWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedAccessProfile>());
        var mapper = config.CreateMapper();

        var source = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Department = "Engineering",
            Salary = 75000.50m
        };

        // Act
        var result = mapper.Map<EmployeeDto>(source);

        // Assert
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("Engineering", result.Department);
        Assert.Contains("75", result.SalaryFormatted); // Currency format varies by locale
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void CodeGen_BaseClassProperties_WithCollection_ShouldMapAllItems()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BaseClassPropertyProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<DerivedEntity>
        {
            new() { Id = 1, Name = "First", CreatedAt = new DateTime(2024, 1, 1), Description = "D1", IsActive = true },
            new() { Id = 2, Name = "Second", CreatedAt = new DateTime(2024, 2, 2), Description = "D2", IsActive = false },
            new() { Id = 3, Name = "Third", CreatedAt = new DateTime(2024, 3, 3), Description = "D3", IsActive = true }
        };

        // Act
        var results = mapper.Map<List<DerivedEntityDto>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].EntityId);
        Assert.Equal(2, results[1].EntityId);
        Assert.Equal(3, results[2].EntityId);
        Assert.Equal("First", results[0].EntityName);
        Assert.Equal("Second", results[1].EntityName);
        Assert.Equal("Third", results[2].EntityName);
    }

    #endregion
}
