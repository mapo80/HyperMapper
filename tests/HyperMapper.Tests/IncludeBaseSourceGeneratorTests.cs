using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v9.0.0: Unit tests for IncludeBase&lt;TBaseSource, TBaseDest&gt;() in Source Generator.
/// Tests configuration inheritance from base type mapping.
/// </summary>
public class IncludeBaseSourceGeneratorTests
{
    #region Test Types

    public class BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DerivedEntity : BaseEntity
    {
        public string Description { get; set; } = "";
    }

    public class BaseEntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DerivedEntityDto : BaseEntityDto
    {
        public string Description { get; set; } = "";
    }

    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class Employee : Person
    {
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
    }

    public class PersonDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class EmployeeDto : PersonDto
    {
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
    }

    public class Vehicle
    {
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
    }

    public class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    public class VehicleDto
    {
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
    }

    public class CarDto : VehicleDto
    {
        public int Doors { get; set; }
    }

    #endregion

    #region Test Profiles

    public class BasicIncludeBaseProfile : Profile
    {
        public BasicIncludeBaseProfile()
        {
            CreateMap<BaseEntity, BaseEntityDto>();

            CreateMap<DerivedEntity, DerivedEntityDto>()
                .IncludeBase<BaseEntity, BaseEntityDto>();
        }
    }

    public class PersonEmployeeProfile : Profile
    {
        public PersonEmployeeProfile()
        {
            CreateMap<Person, PersonDto>();

            CreateMap<Employee, EmployeeDto>()
                .IncludeBase<Person, PersonDto>();
        }
    }

    public class WithCustomMappingProfile : Profile
    {
        public WithCustomMappingProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(d => d.Make, opt => opt.MapFrom(s => s.Make.ToUpper()));

            CreateMap<Car, CarDto>()
                .IncludeBase<Vehicle, VehicleDto>()
                .ForMember(d => d.Doors, opt => opt.MapFrom(s => s.Doors * 2));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void IncludeBase_InheritsBaseConfiguration()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicIncludeBaseProfile>());
        var mapper = config.CreateMapper();
        var source = new DerivedEntity { Id = 1, Name = "Test", Description = "Description" };

        // Act
        var dest = mapper.Map<DerivedEntityDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
        Assert.Equal("Description", dest.Description);
    }

    [Fact]
    public void IncludeBase_PersonEmployee_MapsAllProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PersonEmployeeProfile>());
        var mapper = config.CreateMapper();
        var source = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Department = "Engineering",
            Salary = 75000
        };

        // Act
        var dest = mapper.Map<EmployeeDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.FirstName);
        Assert.Equal("Doe", dest.LastName);
        Assert.Equal("Engineering", dest.Department);
        Assert.Equal(75000, dest.Salary);
    }

    [Fact]
    public void IncludeBase_BaseMappingStillWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicIncludeBaseProfile>());
        var mapper = config.CreateMapper();
        var source = new BaseEntity { Id = 42, Name = "Base Only" };

        // Act
        var dest = mapper.Map<BaseEntityDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Id);
        Assert.Equal("Base Only", dest.Name);
    }

    [Fact]
    public void IncludeBase_WithCustomMapping_BothApply()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<WithCustomMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new Car { Make = "toyota", Model = "Camry", Doors = 4 };

        // Act
        var dest = mapper.Map<CarDto>(source);

        // Assert
        Assert.NotNull(dest);
        // Base mapping should apply ToUpper to Make
        Assert.Equal("TOYOTA", dest.Make);
        Assert.Equal("Camry", dest.Model);
        // Derived mapping should double the doors
        Assert.Equal(8, dest.Doors);
    }

    [Fact]
    public void IncludeBase_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicIncludeBaseProfile>());
        var mapper = config.CreateMapper();
        DerivedEntity? nullEntity = null;

        // Act
        var dest = mapper.Map<DerivedEntityDto>(nullEntity!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void IncludeBase_DirectBaseMapping_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PersonEmployeeProfile>());
        var mapper = config.CreateMapper();
        var source = new Person { FirstName = "Jane", LastName = "Smith" };

        // Act
        var dest = mapper.Map<PersonDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Jane", dest.FirstName);
        Assert.Equal("Smith", dest.LastName);
    }

    #endregion
}
