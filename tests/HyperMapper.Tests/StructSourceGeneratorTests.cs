using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for Source Generator support of value types (structs).
/// These tests verify that struct mapping works correctly at compile-time.
/// </summary>
public class StructSourceGeneratorTests
{
    #region Test Types

    public struct SourcePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct DestPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct SourcePointWithLong
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct DestPointWithLong
    {
        public long X { get; set; }
        public long Y { get; set; }
    }

    public struct SourceStruct
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    public class DestClass
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    public class SourceClass
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    public struct DestStruct
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    public struct NestedSourceStruct
    {
        public SourcePoint Point { get; set; }
        public string? Label { get; set; }
    }

    public struct NestedDestStruct
    {
        public DestPoint Point { get; set; }
        public string? Label { get; set; }
    }

    public struct StructWithCollection
    {
        public int Id { get; set; }
        public List<string>? Items { get; set; }
    }

    public struct DestStructWithCollection
    {
        public int Id { get; set; }
        public List<string>? Items { get; set; }
    }

    public struct StructWithComplexProperty
    {
        public int Id { get; set; }
        public SourceClass? ComplexProperty { get; set; }
    }

    public struct DestStructWithComplexProperty
    {
        public int Id { get; set; }
        public DestClass? ComplexProperty { get; set; }
    }

    public readonly struct ReadOnlySourceStruct
    {
        public int Value { get; init; }
        public string? Name { get; init; }
    }

    public struct ReadOnlyDestStruct
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    #endregion

    #region Test Profiles

    public class StructToStructProfile : Profile
    {
        public StructToStructProfile()
        {
            CreateMap<SourcePoint, DestPoint>();
        }
    }

    public class StructWithConversionProfile : Profile
    {
        public StructWithConversionProfile()
        {
            CreateMap<SourcePointWithLong, DestPointWithLong>();
        }
    }

    public class StructToClassProfile : Profile
    {
        public StructToClassProfile()
        {
            CreateMap<SourceStruct, DestClass>();
        }
    }

    public class ClassToStructProfile : Profile
    {
        public ClassToStructProfile()
        {
            CreateMap<SourceClass, DestStruct>();
        }
    }

    public class NullableStructProfile : Profile
    {
        public NullableStructProfile()
        {
            CreateMap<SourcePoint, DestPoint>();
        }
    }

    public class NestedStructProfile : Profile
    {
        public NestedStructProfile()
        {
            CreateMap<SourcePoint, DestPoint>();
            CreateMap<NestedSourceStruct, NestedDestStruct>();
        }
    }

    public class StructCollectionProfile : Profile
    {
        public StructCollectionProfile()
        {
            CreateMap<SourcePoint, DestPoint>();
        }
    }

    public class StructWithCollectionPropertyProfile : Profile
    {
        public StructWithCollectionPropertyProfile()
        {
            CreateMap<StructWithCollection, DestStructWithCollection>();
        }
    }

    public class StructWithComplexPropertyProfile : Profile
    {
        public StructWithComplexPropertyProfile()
        {
            CreateMap<SourceClass, DestClass>();
            CreateMap<StructWithComplexProperty, DestStructWithComplexProperty>();
        }
    }

    public class ReadOnlyStructProfile : Profile
    {
        public ReadOnlyStructProfile()
        {
            CreateMap<ReadOnlySourceStruct, ReadOnlyDestStruct>();
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void StructToStruct_SameProperties_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructToStructProfile>());
        var mapper = config.CreateMapper();
        var source = new SourcePoint { X = 10, Y = 20 };

        // Act
        var dest = mapper.Map<DestPoint>(source);

        // Assert
        Assert.Equal(10, dest.X);
        Assert.Equal(20, dest.Y);
    }

    [Fact]
    public void StructToStruct_DifferentPropertyTypes_MapsWithConversion()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructWithConversionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourcePointWithLong { X = 100, Y = 200 };

        // Act
        var dest = mapper.Map<DestPointWithLong>(source);

        // Assert
        Assert.Equal(100L, dest.X);
        Assert.Equal(200L, dest.Y);
    }

    [Fact]
    public void StructToClass_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructToClassProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceStruct { Value = 42, Name = "Test" };

        // Act
        var dest = mapper.Map<DestClass>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void ClassToStruct_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ClassToStructProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceClass { Value = 42, Name = "Test" };

        // Act
        var dest = mapper.Map<DestStruct>(source);

        // Assert
        Assert.Equal(42, dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void NullableStructToStruct_WithValue_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();
        SourcePoint? source = new SourcePoint { X = 10, Y = 20 };

        // Act
        var dest = mapper.Map<DestPoint?>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.Value.X);
        Assert.Equal(20, dest.Value.Y);
    }

    [Fact]
    public void NullableStructToStruct_WithNull_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();
        SourcePoint? source = null;

        // Act
        var dest = mapper.Map<DestPoint?>(source!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void StructToNullableStruct_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();
        var source = new SourcePoint { X = 10, Y = 20 };

        // Act
        var dest = mapper.Map<DestPoint?>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.Value.X);
        Assert.Equal(20, dest.Value.Y);
    }

    [Fact]
    public void NullableStructToNullableStruct_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();
        SourcePoint? source = new SourcePoint { X = 10, Y = 20 };

        // Act
        var dest = mapper.Map<DestPoint?>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.Value.X);
        Assert.Equal(20, dest.Value.Y);
    }

    [Fact]
    public void StructWithNestedStruct_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedStructProfile>());
        var mapper = config.CreateMapper();
        var source = new NestedSourceStruct
        {
            Point = new SourcePoint { X = 10, Y = 20 },
            Label = "Test"
        };

        // Act
        var dest = mapper.Map<NestedDestStruct>(source);

        // Assert
        Assert.Equal(10, dest.Point.X);
        Assert.Equal(20, dest.Point.Y);
        Assert.Equal("Test", dest.Label);
    }

    [Fact]
    public void StructCollection_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructCollectionProfile>());
        var mapper = config.CreateMapper();
        var source = new List<SourcePoint>
        {
            new SourcePoint { X = 1, Y = 2 },
            new SourcePoint { X = 3, Y = 4 },
            new SourcePoint { X = 5, Y = 6 }
        };

        // Act
        var dest = mapper.Map<List<DestPoint>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(3, dest.Count);
        Assert.Equal(1, dest[0].X);
        Assert.Equal(2, dest[0].Y);
        Assert.Equal(3, dest[1].X);
        Assert.Equal(4, dest[1].Y);
        Assert.Equal(5, dest[2].X);
        Assert.Equal(6, dest[2].Y);
    }

    [Fact]
    public void StructWithCollectionProperty_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructWithCollectionPropertyProfile>());
        var mapper = config.CreateMapper();
        var source = new StructWithCollection
        {
            Id = 1,
            Items = new List<string> { "A", "B", "C" }
        };

        // Act
        var dest = mapper.Map<DestStructWithCollection>(source);

        // Assert
        Assert.Equal(1, dest.Id);
        Assert.NotNull(dest.Items);
        Assert.Equal(3, dest.Items.Count);
        Assert.Equal("A", dest.Items[0]);
    }

    [Fact]
    public void StructWithComplexProperties_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StructWithComplexPropertyProfile>());
        var mapper = config.CreateMapper();
        var source = new StructWithComplexProperty
        {
            Id = 1,
            ComplexProperty = new SourceClass { Value = 42, Name = "Nested" }
        };

        // Act
        var dest = mapper.Map<DestStructWithComplexProperty>(source);

        // Assert
        Assert.Equal(1, dest.Id);
        Assert.NotNull(dest.ComplexProperty);
        Assert.Equal(42, dest.ComplexProperty.Value);
        Assert.Equal("Nested", dest.ComplexProperty.Name);
    }

    [Fact]
    public void ReadOnlyStruct_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReadOnlyStructProfile>());
        var mapper = config.CreateMapper();
        var source = new ReadOnlySourceStruct { Value = 42, Name = "ReadOnly" };

        // Act
        var dest = mapper.Map<ReadOnlyDestStruct>(source);

        // Assert
        Assert.Equal(42, dest.Value);
        Assert.Equal("ReadOnly", dest.Name);
    }

    #endregion
}
