using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v12.0.0: Tests for Source Generator support of class-based ITypeConverter.
/// These tests verify that ConvertUsing(new MyConverter()) generates proper compile-time code.
/// </summary>
public class ClassBasedConverterSourceGeneratorTests
{
    #region Test Types

    public class SourcePoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class DestPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class PointConverter : ITypeConverter<SourcePoint?, DestPointDto?>
    {
        public DestPointDto? Convert(SourcePoint? source, DestPointDto? destination, ResolutionContext context)
        {
            if (source == null) return null;
            return new DestPointDto { Latitude = source.Y, Longitude = source.X };
        }
    }

    public class ReversePointConverter : ITypeConverter<DestPointDto?, SourcePoint?>
    {
        public SourcePoint? Convert(DestPointDto? source, SourcePoint? destination, ResolutionContext context)
        {
            if (source == null) return null;
            return new SourcePoint { X = source.Longitude, Y = source.Latitude };
        }
    }

    public class SourceWithPoint
    {
        public int Id { get; set; }
        public SourcePoint? Location { get; set; }
    }

    public class DestWithPointDto
    {
        public int Id { get; set; }
        public DestPointDto? Location { get; set; }
    }

    #endregion

    #region Test Profiles

    public class PointConverterProfile : Profile
    {
        public PointConverterProfile()
        {
            CreateMap<SourcePoint, DestPointDto>().ConvertUsing(new PointConverter());
            CreateMap<DestPointDto, SourcePoint>().ConvertUsing(new ReversePointConverter());
        }
    }

    public class NestedPointConverterProfile : Profile
    {
        public NestedPointConverterProfile()
        {
            CreateMap<SourcePoint, DestPointDto>().ConvertUsing(new PointConverter());
            CreateMap<SourceWithPoint, DestWithPointDto>();
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void ClassBasedConverter_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PointConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new SourcePoint { X = 10.5, Y = 20.5 };

        // Act
        var result = mapper.Map<DestPointDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20.5, result.Latitude);
        Assert.Equal(10.5, result.Longitude);
    }

    [Fact]
    public void ClassBasedConverter_HandlesNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PointConverterProfile>());
        var mapper = config.CreateMapper();

        SourcePoint? source = null;

        // Act
        var result = mapper.Map<DestPointDto?>(source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ClassBasedConverter_ReverseMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PointConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new DestPointDto { Latitude = 30.0, Longitude = 40.0 };

        // Act
        var result = mapper.Map<SourcePoint>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(40.0, result.X);
        Assert.Equal(30.0, result.Y);
    }

    [Fact]
    public void ClassBasedConverter_NestedProperty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedPointConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithPoint
        {
            Id = 1,
            Location = new SourcePoint { X = 5.0, Y = 10.0 }
        };

        // Act
        var result = mapper.Map<DestWithPointDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Location);
        Assert.Equal(10.0, result.Location.Latitude);
        Assert.Equal(5.0, result.Location.Longitude);
    }

    [Fact]
    public void ClassBasedConverter_NestedPropertyNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedPointConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithPoint
        {
            Id = 2,
            Location = null
        };

        // Act
        var result = mapper.Map<DestWithPointDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Null(result.Location);
    }

    #endregion
}
