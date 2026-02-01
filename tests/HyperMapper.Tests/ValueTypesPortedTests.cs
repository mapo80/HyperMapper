using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 ValueTypes.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class ValueTypesPortedTests
{
    #region Basic Struct Mapping Tests

    [Fact]
    public void Should_map_struct_to_struct()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceStruct { Value = 42, Name = "Test" };
        var dest = mapper.Map<DestStruct>(source);

        Assert.Equal(42, dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Should_map_struct_to_class()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceStruct { Value = 42, Name = "Test" };
        var dest = mapper.Map<DestClass>(source);

        Assert.Equal(42, dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Should_map_class_to_struct()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceClass { Value = 42, Name = "Test" };
        var dest = mapper.Map<DestStruct>(source);

        Assert.Equal(42, dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    #endregion

    #region Struct With Custom Mapping Tests

    [Fact]
    public void Should_map_struct_with_custom_mapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CustomStructProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomSourceStruct { A = 10, B = 20 };
        var dest = mapper.Map<CustomDestStruct>(source);

        Assert.Equal(30, dest.Sum);
    }

    #endregion

    #region Nullable Struct Mapping Tests

    [Fact]
    public void Should_map_nullable_struct_with_value()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableStructSource { Point = new Point { X = 1, Y = 2 } };
        var dest = mapper.Map<NullableStructDest>(source);

        Assert.NotNull(dest.Point);
        Assert.Equal(1, dest.Point.Value.X);
        Assert.Equal(2, dest.Point.Value.Y);
    }

    [Fact]
    public void Should_map_nullable_struct_null_value()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableStructSource { Point = null };
        var dest = mapper.Map<NullableStructDest>(source);

        Assert.Null(dest.Point);
    }

    [Fact]
    public void Should_map_struct_to_nullable_struct()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableStructProfile>());
        var mapper = config.CreateMapper();

        var source = new NonNullableStructSource { Point = new Point { X = 5, Y = 10 } };
        var dest = mapper.Map<NullableStructDest>(source);

        Assert.NotNull(dest.Point);
        Assert.Equal(5, dest.Point.Value.X);
        Assert.Equal(10, dest.Point.Value.Y);
    }

    #endregion

    #region Struct Collection Mapping Tests

    [Fact]
    public void Should_map_collection_of_structs()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new StructCollectionSource
        {
            Points = new List<Point>
            {
                new() { X = 1, Y = 2 },
                new() { X = 3, Y = 4 }
            }
        };

        var dest = mapper.Map<StructCollectionDest>(source);

        Assert.Equal(2, dest.Points.Count);
        Assert.Equal(1, dest.Points[0].X);
        Assert.Equal(3, dest.Points[1].X);
    }

    [Fact]
    public void Should_map_array_of_structs()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new StructArraySource
        {
            Points = new Point[]
            {
                new() { X = 1, Y = 2 },
                new() { X = 3, Y = 4 }
            }
        };

        var dest = mapper.Map<StructArrayDest>(source);

        Assert.Equal(2, dest.Points.Length);
        Assert.Equal(1, dest.Points[0].X);
        Assert.Equal(3, dest.Points[1].X);
    }

    #endregion

    #region Record Struct Mapping Tests

    [Fact]
    public void Should_map_record_struct()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<RecordStructProfile>());
        var mapper = config.CreateMapper();

        var source = new RecordStructSource(42, "Test");
        var dest = mapper.Map<RecordStructDest>(source);

        Assert.Equal(42, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    #endregion

    #region Struct With Nested Objects Tests

    [Fact]
    public void Should_map_struct_with_nested_class()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructWithNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new StructWithNestedSource
        {
            Id = 1,
            Nested = new NestedClass { Value = "Nested Value" }
        };

        var dest = mapper.Map<StructWithNestedDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.NotNull(dest.Nested);
        Assert.Equal("Nested Value", dest.Nested.Value);
    }

    [Fact]
    public void Should_map_struct_with_null_nested()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructWithNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new StructWithNestedSource
        {
            Id = 1,
            Nested = null
        };

        var dest = mapper.Map<StructWithNestedDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Null(dest.Nested);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Struct
public struct SourceStruct
{
    public int Value { get; set; }
    public string Name { get; set; }
}

public struct DestStruct
{
    public int Value { get; set; }
    public string Name { get; set; }
}

public class SourceClass
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DestClass
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StructProfile : Profile
{
    public StructProfile()
    {
        CreateMap<SourceStruct, DestStruct>();
        CreateMap<SourceStruct, DestClass>();
        CreateMap<SourceClass, DestStruct>();
    }
}

// Custom Struct Mapping
public struct CustomSourceStruct
{
    public int A { get; set; }
    public int B { get; set; }
}

public struct CustomDestStruct
{
    public int Sum { get; set; }
}

public class CustomStructProfile : Profile
{
    public CustomStructProfile()
    {
        CreateMap<CustomSourceStruct, CustomDestStruct>()
            .ForMember(d => d.Sum, opt => opt.MapFrom(s => s.A + s.B));
    }
}

// Point struct for nullable tests
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}

public struct PointDto
{
    public int X { get; set; }
    public int Y { get; set; }
}

// Nullable Struct
public class NullableStructSource
{
    public Point? Point { get; set; }
}

public class NullableStructDest
{
    public PointDto? Point { get; set; }
}

public class NonNullableStructSource
{
    public Point Point { get; set; }
}

public class NullableStructProfile : Profile
{
    public NullableStructProfile()
    {
        CreateMap<Point, PointDto>();
        CreateMap<NullableStructSource, NullableStructDest>();
        CreateMap<NonNullableStructSource, NullableStructDest>()
            .ForMember(d => d.Point, opt => opt.MapFrom(s => (PointDto?)mapper_Map(s.Point)));
    }

    private PointDto mapper_Map(Point p) => new PointDto { X = p.X, Y = p.Y };
}

// Struct Collection
public class StructCollectionSource
{
    public List<Point> Points { get; set; } = new();
}

public class StructCollectionDest
{
    public List<PointDto> Points { get; set; } = new();
}

public class StructArraySource
{
    public Point[] Points { get; set; } = Array.Empty<Point>();
}

public class StructArrayDest
{
    public PointDto[] Points { get; set; } = Array.Empty<PointDto>();
}

public class StructCollectionProfile : Profile
{
    public StructCollectionProfile()
    {
        CreateMap<Point, PointDto>();
        CreateMap<StructCollectionSource, StructCollectionDest>();
        CreateMap<StructArraySource, StructArrayDest>();
    }
}

// Record Struct
public readonly record struct RecordStructSource(int Id, string Name);

public class RecordStructDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RecordStructProfile : Profile
{
    public RecordStructProfile()
    {
        CreateMap<RecordStructSource, RecordStructDest>();
    }
}

// Struct with Nested
public struct StructWithNestedSource
{
    public int Id { get; set; }
    public NestedClass? Nested { get; set; }
}

public class NestedClass
{
    public string Value { get; set; } = string.Empty;
}

public struct StructWithNestedDest
{
    public int Id { get; set; }
    public NestedClassDest? Nested { get; set; }
}

public class NestedClassDest
{
    public string Value { get; set; } = string.Empty;
}

public class StructWithNestedProfile : Profile
{
    public StructWithNestedProfile()
    {
        CreateMap<NestedClass, NestedClassDest>();
        CreateMap<StructWithNestedSource, StructWithNestedDest>();
    }
}

#endregion
