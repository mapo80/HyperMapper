using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v12.1.0: Tests for Source Generator support of ConvertUsing(typeof(MyConverter)).
/// These tests verify that ConvertUsing(typeof(MyConverter)) generates proper compile-time code.
/// </summary>
public class TypeOfConverterSourceGeneratorTests
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

    // String to List converter - simulates the real use case from riconoscimento-documenti-api
    public class StringToListConverter : ITypeConverter<string?, IList<DestPointDto>?>
    {
        public IList<DestPointDto>? Convert(string? source, IList<DestPointDto>? destination, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source)) return null;

            // Parse format: "x1,y1;x2,y2;x3,y3"
            var points = new List<DestPointDto>();
            var pairs = source.Split(';');
            foreach (var pair in pairs)
            {
                var coords = pair.Split(',');
                if (coords.Length == 2 &&
                    double.TryParse(coords[0], out var x) &&
                    double.TryParse(coords[1], out var y))
                {
                    points.Add(new DestPointDto { Latitude = y, Longitude = x });
                }
            }
            return points.Count > 0 ? points : null;
        }
    }

    public class SourceWithBoundingBox
    {
        public int Id { get; set; }
        public string? BoundingBox { get; set; }
    }

    public class DestWithBoundingBoxDto
    {
        public int Id { get; set; }
        public IList<DestPointDto>? BoundingBox { get; set; }
    }

    #endregion

    #region Test Profiles

    public class TypeOfPointConverterProfile : Profile
    {
        public TypeOfPointConverterProfile()
        {
            CreateMap<SourcePoint, DestPointDto>().ConvertUsing(typeof(PointConverter));
            CreateMap<DestPointDto, SourcePoint>().ConvertUsing(typeof(ReversePointConverter));
        }
    }

    public class TypeOfNestedPointConverterProfile : Profile
    {
        public TypeOfNestedPointConverterProfile()
        {
            CreateMap<SourcePoint, DestPointDto>().ConvertUsing(typeof(PointConverter));
            CreateMap<SourceWithPoint, DestWithPointDto>();
        }
    }

    public class TypeOfStringToListConverterProfile : Profile
    {
        public TypeOfStringToListConverterProfile()
        {
            CreateMap<string, IList<DestPointDto>?>().ConvertUsing(typeof(StringToListConverter));
            CreateMap<SourceWithBoundingBox, DestWithBoundingBoxDto>();
        }
    }

    #endregion

    #region Tests - Basic Mapping with typeof()

    [Fact]
    public void TypeOfConverter_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfPointConverterProfile>());
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
    public void TypeOfConverter_HandlesNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfPointConverterProfile>());
        var mapper = config.CreateMapper();

        SourcePoint? source = null;

        // Act
        var result = mapper.Map<DestPointDto?>(source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TypeOfConverter_ReverseMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfPointConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new DestPointDto { Latitude = 30.0, Longitude = 40.0 };

        // Act
        var result = mapper.Map<SourcePoint>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(40.0, result.X);
        Assert.Equal(30.0, result.Y);
    }

    #endregion

    #region Tests - Nested Property with typeof()

    [Fact]
    public void TypeOfConverter_NestedProperty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfNestedPointConverterProfile>());
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
    public void TypeOfConverter_NestedPropertyNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfNestedPointConverterProfile>());
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

    #region Tests - String to List Converter (Real Use Case)

    [Fact]
    public void TypeOfConverter_StringToList_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());
        var mapper = config.CreateMapper();

        var source = "10.5,20.5;30.5,40.5";

        // Act
        var result = mapper.Map<IList<DestPointDto>?>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(20.5, result[0].Latitude);
        Assert.Equal(10.5, result[0].Longitude);
        Assert.Equal(40.5, result[1].Latitude);
        Assert.Equal(30.5, result[1].Longitude);
    }

    [Fact]
    public void TypeOfConverter_StringToList_HandlesNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());
        var mapper = config.CreateMapper();

        string? source = null;

        // Act
        var result = mapper.Map<IList<DestPointDto>?>(source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TypeOfConverter_StringToList_HandlesEmpty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());
        var mapper = config.CreateMapper();

        var source = "";

        // Act
        var result = mapper.Map<IList<DestPointDto>?>(source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TypeOfConverter_StringToList_NestedProperty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithBoundingBox
        {
            Id = 1,
            BoundingBox = "1.0,2.0;3.0,4.0;5.0,6.0"
        };

        // Act
        var result = mapper.Map<DestWithBoundingBoxDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.BoundingBox);
        Assert.Equal(3, result.BoundingBox.Count);
        Assert.Equal(2.0, result.BoundingBox[0].Latitude);
        Assert.Equal(1.0, result.BoundingBox[0].Longitude);
    }

    [Fact]
    public void TypeOfConverter_StringToList_DirectNullMapping()
    {
        // This test verifies the converter is properly called for direct null mapping
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());
        var mapper = config.CreateMapper();

        // Direct mapping of null string to IList
        string? nullString = null;
        var result = mapper.Map<IList<DestPointDto>?>(nullString);

        // The converter should return null for null input
        Assert.Null(result);
    }

    [Fact]
    public void TypeOfConverter_StringToList_TypeMapExists()
    {
        // Verify that the TypeMap is properly registered
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());

        // Use reflection to verify the TypeMap exists with converter
        var registry = config.GetType().GetField("_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(registry);
        var registryValue = registry!.GetValue(config);
        Assert.NotNull(registryValue);

        var findTypeMapMethod = registryValue!.GetType().GetMethod("FindTypeMap");
        Assert.NotNull(findTypeMapMethod);

        var typeMap = findTypeMapMethod!.Invoke(registryValue, new object[] { typeof(string), typeof(IList<DestPointDto>) });
        Assert.NotNull(typeMap);

        var converterTypeProp = typeMap!.GetType().GetProperty("ConverterType");
        Assert.NotNull(converterTypeProp);

        var converterType = converterTypeProp!.GetValue(typeMap);
        Assert.NotNull(converterType);
        Assert.Equal(typeof(StringToListConverter), converterType);
    }

    [Fact]
    public void TypeOfConverter_StringToList_NestedPropertyNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TypeOfStringToListConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithBoundingBox
        {
            Id = 2,
            BoundingBox = null
        };

        // Act
        var result = mapper.Map<DestWithBoundingBoxDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        // When source property BoundingBox is null and there's a registered converter,
        // the converter should be called and it returns null for null input.
        Assert.Null(result.BoundingBox);
    }

    #endregion
}
