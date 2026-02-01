using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v9.0.0: Unit tests for Include&lt;TDerivedSource, TDerivedDest&gt;() in Source Generator.
/// Tests polymorphic mapping at compile-time code generation.
/// </summary>
public class IncludeSourceGeneratorTests
{
    #region Test Types

    public class Vehicle
    {
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
    }

    public class Car : Vehicle
    {
        public int Doors { get; set; }
    }

    public class Motorcycle : Vehicle
    {
        public bool HasSidecar { get; set; }
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

    public class MotorcycleDto : VehicleDto
    {
        public bool HasSidecar { get; set; }
    }

    public class Animal
    {
        public string Name { get; set; } = "";
    }

    public class Dog : Animal
    {
        public string Breed { get; set; } = "";
    }

    public class Cat : Animal
    {
        public bool IsIndoor { get; set; }
    }

    public class AnimalDto
    {
        public string Name { get; set; } = "";
    }

    public class DogDto : AnimalDto
    {
        public string Breed { get; set; } = "";
    }

    public class CatDto : AnimalDto
    {
        public bool IsIndoor { get; set; }
    }

    #endregion

    #region Test Profiles

    public class VehicleIncludeProfile : Profile
    {
        public VehicleIncludeProfile()
        {
            CreateMap<Vehicle, VehicleDto>()
                .Include<Car, CarDto>()
                .Include<Motorcycle, MotorcycleDto>();

            CreateMap<Car, CarDto>();
            CreateMap<Motorcycle, MotorcycleDto>();
        }
    }

    public class SingleIncludeProfile : Profile
    {
        public SingleIncludeProfile()
        {
            CreateMap<Animal, AnimalDto>()
                .Include<Dog, DogDto>();

            CreateMap<Dog, DogDto>();
        }
    }

    public class MultipleIncludeProfile : Profile
    {
        public MultipleIncludeProfile()
        {
            CreateMap<Animal, AnimalDto>()
                .Include<Dog, DogDto>()
                .Include<Cat, CatDto>();

            CreateMap<Dog, DogDto>();
            CreateMap<Cat, CatDto>();
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void Include_BaseTypeDirect_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VehicleIncludeProfile>());
        var mapper = config.CreateMapper();
        var source = new Vehicle { Make = "Generic", Model = "Base" };

        // Act
        var dest = mapper.Map<VehicleDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.IsType<VehicleDto>(dest);
        Assert.Equal("Generic", dest.Make);
        Assert.Equal("Base", dest.Model);
    }

    [Fact]
    public void Include_DerivedCar_MapsToCarDto()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VehicleIncludeProfile>());
        var mapper = config.CreateMapper();
        Vehicle source = new Car { Make = "Toyota", Model = "Camry", Doors = 4 };

        // Act
        var dest = mapper.Map<VehicleDto>(source);

        // Assert
        Assert.NotNull(dest);
        var carDto = Assert.IsType<CarDto>(dest);
        Assert.Equal("Toyota", carDto.Make);
        Assert.Equal("Camry", carDto.Model);
        Assert.Equal(4, carDto.Doors);
    }

    [Fact]
    public void Include_DerivedMotorcycle_MapsToMotorcycleDto()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VehicleIncludeProfile>());
        var mapper = config.CreateMapper();
        Vehicle source = new Motorcycle { Make = "Harley", Model = "Davidson", HasSidecar = true };

        // Act
        var dest = mapper.Map<VehicleDto>(source);

        // Assert
        Assert.NotNull(dest);
        var motoDto = Assert.IsType<MotorcycleDto>(dest);
        Assert.Equal("Harley", motoDto.Make);
        Assert.Equal("Davidson", motoDto.Model);
        Assert.True(motoDto.HasSidecar);
    }

    [Fact]
    public void Include_SingleDerived_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleIncludeProfile>());
        var mapper = config.CreateMapper();
        Animal source = new Dog { Name = "Rex", Breed = "German Shepherd" };

        // Act
        var dest = mapper.Map<AnimalDto>(source);

        // Assert
        Assert.NotNull(dest);
        var dogDto = Assert.IsType<DogDto>(dest);
        Assert.Equal("Rex", dogDto.Name);
        Assert.Equal("German Shepherd", dogDto.Breed);
    }

    [Fact]
    public void Include_MultipleIncludes_AllWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleIncludeProfile>());
        var mapper = config.CreateMapper();

        Animal dog = new Dog { Name = "Rex", Breed = "Labrador" };
        Animal cat = new Cat { Name = "Whiskers", IsIndoor = true };

        // Act
        var dogResult = mapper.Map<AnimalDto>(dog);
        var catResult = mapper.Map<AnimalDto>(cat);

        // Assert
        var dogDto = Assert.IsType<DogDto>(dogResult);
        Assert.Equal("Rex", dogDto.Name);
        Assert.Equal("Labrador", dogDto.Breed);

        var catDto = Assert.IsType<CatDto>(catResult);
        Assert.Equal("Whiskers", catDto.Name);
        Assert.True(catDto.IsIndoor);
    }

    [Fact]
    public void Include_DirectCarMapping_StillWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VehicleIncludeProfile>());
        var mapper = config.CreateMapper();
        var source = new Car { Make = "Honda", Model = "Civic", Doors = 2 };

        // Act - Map directly as Car, not as Vehicle
        var dest = mapper.Map<CarDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Honda", dest.Make);
        Assert.Equal("Civic", dest.Model);
        Assert.Equal(2, dest.Doors);
    }

    [Fact]
    public void Include_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VehicleIncludeProfile>());
        var mapper = config.CreateMapper();
        Vehicle? nullVehicle = null;

        // Act
        var dest = mapper.Map<VehicleDto>(nullVehicle!);

        // Assert
        Assert.Null(dest);
    }

    #endregion
}
