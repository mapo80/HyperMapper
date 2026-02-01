using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 Include()/IncludeBase() feature - inheritance mapping.
/// AutoMapper API compatibility:
/// - CreateMap<Animal, AnimalDto>().Include<Dog, DogDto>()
/// - CreateMap<Dog, DogDto>().IncludeBase<Animal, AnimalDto>()
/// </summary>
public class IncludeIncludeBaseTests
{
    #region Test Models

    public abstract class Animal
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class Dog : Animal
    {
        public string? Breed { get; set; }
        public bool CanFetch { get; set; }
    }

    public class Cat : Animal
    {
        public bool IsIndoor { get; set; }
        public int LivesRemaining { get; set; }
    }

    public class Bird : Animal
    {
        public double Wingspan { get; set; }
        public bool CanFly { get; set; }
    }

    public abstract class AnimalDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class DogDto : AnimalDto
    {
        public string? Breed { get; set; }
        public bool CanFetch { get; set; }
    }

    public class CatDto : AnimalDto
    {
        public bool IsIndoor { get; set; }
        public int LivesRemaining { get; set; }
    }

    public class BirdDto : AnimalDto
    {
        public double Wingspan { get; set; }
        public bool CanFly { get; set; }
    }

    // For testing with concrete base classes
    public class Vehicle
    {
        public int Id { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
    }

    public class Car : Vehicle
    {
        public int Doors { get; set; }
        public bool IsElectric { get; set; }
    }

    public class Motorcycle : Vehicle
    {
        public int EngineCC { get; set; }
        public bool HasSidecar { get; set; }
    }

    public class VehicleDto
    {
        public int Id { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
    }

    public class CarDto : VehicleDto
    {
        public int Doors { get; set; }
        public bool IsElectric { get; set; }
    }

    public class MotorcycleDto : VehicleDto
    {
        public int EngineCC { get; set; }
        public bool HasSidecar { get; set; }
    }

    // For testing deep inheritance
    public class SportsCar : Car
    {
        public int Horsepower { get; set; }
        public decimal TopSpeed { get; set; }
    }

    public class SportsCarDto : CarDto
    {
        public int Horsepower { get; set; }
        public decimal TopSpeed { get; set; }
    }

    #endregion

    #region Profiles

    public class IncludeProfile : Profile
    {
        public IncludeProfile()
        {
            // Base mapping with Include for derived types
            CreateMap<Vehicle, VehicleDto>()
                .Include<Car, CarDto>()
                .Include<Motorcycle, MotorcycleDto>();

            CreateMap<Car, CarDto>();
            CreateMap<Motorcycle, MotorcycleDto>();
        }
    }

    public class IncludeBaseProfile : Profile
    {
        public IncludeBaseProfile()
        {
            // Base mapping
            CreateMap<Vehicle, VehicleDto>();

            // Derived mappings inherit from base using IncludeBase
            CreateMap<Car, CarDto>()
                .IncludeBase<Vehicle, VehicleDto>();

            CreateMap<Motorcycle, MotorcycleDto>()
                .IncludeBase<Vehicle, VehicleDto>();
        }
    }

    public class IncludeWithForMemberProfile : Profile
    {
        public IncludeWithForMemberProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(d => d.Make, opt => opt.MapFrom(s => s.Make!.ToUpper()))
                .Include<Car, CarDto>();

            CreateMap<Car, CarDto>();
        }
    }

    public class IncludeBaseWithOverrideProfile : Profile
    {
        public IncludeBaseWithOverrideProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(d => d.Make, opt => opt.MapFrom(s => "BASE: " + s.Make));

            CreateMap<Car, CarDto>()
                .IncludeBase<Vehicle, VehicleDto>()
                .ForMember(d => d.Make, opt => opt.MapFrom(s => "CAR: " + s.Make));
        }
    }

    public class DeepInheritanceProfile : Profile
    {
        public DeepInheritanceProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .Include<Car, CarDto>();

            CreateMap<Car, CarDto>()
                .Include<SportsCar, SportsCarDto>();

            CreateMap<SportsCar, SportsCarDto>();
        }
    }

    public class CollectionWithIncludeProfile : Profile
    {
        public CollectionWithIncludeProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .Include<Car, CarDto>()
                .Include<Motorcycle, MotorcycleDto>();

            CreateMap<Car, CarDto>();
            CreateMap<Motorcycle, MotorcycleDto>();
        }
    }

    public class IncludeWithAfterMapProfile : Profile
    {
        public IncludeWithAfterMapProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .AfterMap((s, d) => d.Model = d.Model + " (Mapped)")
                .Include<Car, CarDto>();

            CreateMap<Car, CarDto>();
        }
    }

    public class IncludeBaseWithBeforeMapProfile : Profile
    {
        public IncludeBaseWithBeforeMapProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .BeforeMap((s, d) => { /* Base BeforeMap */ });

            // BeforeMap sets Make to "INIT", but also Ignore() convention so it persists
            CreateMap<Car, CarDto>()
                .IncludeBase<Vehicle, VehicleDto>()
                .BeforeMap((s, d) => d.Make = "INIT")
                .ForMember(d => d.Make, opt => opt.Ignore());
        }
    }

    #endregion

    [Fact]
    public void Include_DerivedType_UsesCorrectMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Tesla", Model = "Model 3", Doors = 4, IsElectric = true };

        // Act - Map as base type but get derived type
        var result = mapper.Map<VehicleDto>((Vehicle)car);

        // Assert - Should be CarDto with all properties mapped
        Assert.IsType<CarDto>(result);
        var carDto = (CarDto)result;
        Assert.Equal(1, carDto.Id);
        Assert.Equal("Tesla", carDto.Make);
        Assert.Equal("Model 3", carDto.Model);
        Assert.Equal(4, carDto.Doors);
        Assert.True(carDto.IsElectric);
    }

    [Fact]
    public void Include_MultipleIncludes_AllWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Ford", Model = "Mustang", Doors = 2, IsElectric = false };
        var motorcycle = new Motorcycle { Id = 2, Make = "Harley", Model = "Sportster", EngineCC = 883, HasSidecar = false };

        // Act
        var carResult = mapper.Map<VehicleDto>((Vehicle)car);
        var motoResult = mapper.Map<VehicleDto>((Vehicle)motorcycle);

        // Assert
        Assert.IsType<CarDto>(carResult);
        Assert.IsType<MotorcycleDto>(motoResult);

        var carDto = (CarDto)carResult;
        Assert.Equal(2, carDto.Doors);

        var motoDto = (MotorcycleDto)motoResult;
        Assert.Equal(883, motoDto.EngineCC);
    }

    [Fact]
    public void Include_RuntimeTypeResolution_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeProfile>());
        var mapper = config.CreateMapper();

        // Store derived type in base type variable
        Vehicle vehicle = new Car { Id = 1, Make = "BMW", Model = "M3", Doors = 4, IsElectric = false };

        // Act
        var result = mapper.Map<VehicleDto>(vehicle);

        // Assert - Runtime type should be detected
        Assert.IsType<CarDto>(result);
    }

    [Fact]
    public void IncludeBase_InheritsConfiguration_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeBaseProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Toyota", Model = "Camry", Doors = 4, IsElectric = false };

        // Act
        var result = mapper.Map<CarDto>(car);

        // Assert - Should have base properties mapped (inherited from Vehicle->VehicleDto)
        Assert.Equal(1, result.Id);
        Assert.Equal("Toyota", result.Make);
        Assert.Equal("Camry", result.Model);
        Assert.Equal(4, result.Doors);
    }

    [Fact]
    public void IncludeBase_CanOverride_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeBaseWithOverrideProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Honda", Model = "Civic", Doors = 4, IsElectric = false };

        // Act
        var result = mapper.Map<CarDto>(car);

        // Assert - Derived ForMember should override base
        Assert.Equal("CAR: Honda", result.Make);
    }

    [Fact]
    public void Include_WithForMember_Combines()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeWithForMemberProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Audi", Model = "A4", Doors = 4, IsElectric = false };

        // Act
        var result = mapper.Map<VehicleDto>((Vehicle)car);

        // Assert - Base ForMember configuration should be inherited
        Assert.IsType<CarDto>(result);
        Assert.Equal("AUDI", result.Make); // From base ForMember
    }

    [Fact]
    public void Include_DeepHierarchy_Resolves()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepInheritanceProfile>());
        var mapper = config.CreateMapper();

        var sportsCar = new SportsCar
        {
            Id = 1, Make = "Ferrari", Model = "F8", Doors = 2, IsElectric = false,
            Horsepower = 710, TopSpeed = 340
        };

        // Act - Map as base Vehicle type
        var result = mapper.Map<VehicleDto>((Vehicle)sportsCar);

        // Assert - Should resolve to deepest derived type
        Assert.IsType<SportsCarDto>(result);
        var sportsCarDto = (SportsCarDto)result;
        Assert.Equal(710, sportsCarDto.Horsepower);
        Assert.Equal(340, sportsCarDto.TopSpeed);
    }

    [Fact]
    public void Include_CollectionOfBase_MapsCorrectTypes()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionWithIncludeProfile>());
        var mapper = config.CreateMapper();

        var vehicles = new List<Vehicle>
        {
            new Car { Id = 1, Make = "Ford", Model = "Focus", Doors = 4, IsElectric = false },
            new Motorcycle { Id = 2, Make = "Yamaha", Model = "R1", EngineCC = 998, HasSidecar = false },
            new Car { Id = 3, Make = "Tesla", Model = "Model S", Doors = 4, IsElectric = true }
        };

        // Act
        var results = mapper.Map<List<VehicleDto>>(vehicles);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.IsType<CarDto>(results[0]);
        Assert.IsType<MotorcycleDto>(results[1]);
        Assert.IsType<CarDto>(results[2]);

        Assert.Equal(4, ((CarDto)results[0]).Doors);
        Assert.Equal(998, ((MotorcycleDto)results[1]).EngineCC);
        Assert.True(((CarDto)results[2]).IsElectric);
    }

    [Fact]
    public void Include_WithAfterMap_Inherits()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeWithAfterMapProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Mazda", Model = "MX-5", Doors = 2, IsElectric = false };

        // Act
        var result = mapper.Map<VehicleDto>((Vehicle)car);

        // Assert - AfterMap from base should be applied
        Assert.IsType<CarDto>(result);
        Assert.Equal("MX-5 (Mapped)", result.Model);
    }

    [Fact]
    public void Include_DerivedNotIncluded_UsesBaseMapping()
    {
        // Arrange - Only base mapping configured, no Include for derived types
        var config = new MapperConfiguration(cfg =>
        {
            // Only base mapping, no Include<Car, CarDto>
            cfg.AddProfile<IncludeBaseProfile>();
        });
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Kia", Model = "Stinger", Doors = 4, IsElectric = false };

        // Act - Map derived type directly
        var result = mapper.Map<CarDto>(car);

        // Assert - Direct Car->CarDto mapping should work via IncludeBase
        Assert.Equal(1, result.Id);
        Assert.Equal("Kia", result.Make);
        Assert.Equal(4, result.Doors);
    }

    [Fact]
    public void Include_DirectMappingStillWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Porsche", Model = "911", Doors = 2, IsElectric = false };

        // Act - Direct mapping (not through base type)
        var result = mapper.Map<CarDto>(car);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Porsche", result.Make);
        Assert.Equal(2, result.Doors);
    }

    [Fact]
    public void IncludeBase_WithBeforeMap_Combined()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeBaseWithBeforeMapProfile>());
        var mapper = config.CreateMapper();

        var car = new Car { Id = 1, Make = "Lexus", Model = "IS", Doors = 4, IsElectric = false };

        // Act
        var result = mapper.Map<CarDto>(car);

        // Assert - Derived BeforeMap should run
        Assert.Equal("INIT", result.Make);
    }

    [Fact]
    public void Include_BaseTypeMapping_ReturnsBaseDto()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeProfile>());
        var mapper = config.CreateMapper();

        // Create a base Vehicle instance (not derived)
        var vehicle = new Vehicle { Id = 1, Make = "Generic", Model = "Vehicle" };

        // Act
        var result = mapper.Map<VehicleDto>(vehicle);

        // Assert - Should return base VehicleDto, not a derived type
        Assert.IsType<VehicleDto>(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Generic", result.Make);
    }

    [Fact]
    public void Include_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeProfile>());
        var mapper = config.CreateMapper();
        Vehicle? nullVehicle = null;

        // Act
        var result = mapper.Map<VehicleDto>(nullVehicle!);

        // Assert
        Assert.Null(result);
    }

    public class MultipleLevelIncludeBaseProfile : Profile
    {
        public MultipleLevelIncludeBaseProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .ForMember(d => d.Make, opt => opt.MapFrom(s => s.Make + " (Vehicle)"));

            CreateMap<Car, CarDto>()
                .IncludeBase<Vehicle, VehicleDto>()
                .ForMember(d => d.Model, opt => opt.MapFrom(s => s.Model + " (Car)"));

            CreateMap<SportsCar, SportsCarDto>()
                .IncludeBase<Car, CarDto>();
        }
    }

    [Fact]
    public void IncludeBase_MultipleLevels_InheritsAll()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleLevelIncludeBaseProfile>());
        var mapper = config.CreateMapper();

        var sportsCar = new SportsCar
        {
            Id = 1, Make = "Lamborghini", Model = "Huracan",
            Doors = 2, IsElectric = false, Horsepower = 640, TopSpeed = 325
        };

        // Act
        var result = mapper.Map<SportsCarDto>(sportsCar);

        // Assert - Should inherit from both Vehicle and Car
        Assert.Equal("Lamborghini (Vehicle)", result.Make);  // From Vehicle
        Assert.Equal("Huracan (Car)", result.Model);          // From Car
        Assert.Equal(640, result.Horsepower);                 // From SportsCar convention
    }
}
