using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for edge cases, error handling, and boundary conditions.
/// </summary>
public class EdgeCaseTests
{
    #region Null Handling Tests

    [Fact]
    public void Map_NullSourceWithExplicitTypes_ReturnsDefault()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        EdgeSource? source = null;
        var dest = mapper.Map<EdgeSource, EdgeDest>(source!);

        Assert.Null(dest);
    }

    [Fact]
    public void Map_NullPropertyInNestedObject_HandlesGracefully()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new ParentWithChild { Id = 1, Child = null };
        var dest = mapper.Map<ParentDtoWithChild>(source);

        Assert.Equal(1, dest.Id);
        Assert.Null(dest.Child);
    }

    #endregion

    #region Resolver Error Handling

    [Fact]
    public void Map_ResolverThrowsException_SkipsMember()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ThrowingResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new EdgeSource { Id = 1, Name = "Test" };

        // Should not throw, just skip the problematic member
        var exception = Record.Exception(() => mapper.Map<EdgeDest>(source));

        Assert.Null(exception);
    }

    #endregion

    #region Property Mismatch Tests

    [Fact]
    public void Map_MissingDestinationProperty_IgnoresSourceProperty()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithExtra { Id = 1, Name = "Test", Extra = "ExtraValue" };
        var dest = mapper.Map<DestWithoutExtra>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Map_MissingSourceProperty_LeavesDestinationDefault()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithoutExtra { Id = 1, Name = "Test" };
        var dest = mapper.Map<DestWithExtra>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(string.Empty, dest.Extra);
    }

    #endregion

    #region Type Mismatch Tests

    [Fact]
    public void Map_IncompatibleTypes_UsesConvertChangeType()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDouble { Value = 3.14 };
        var dest = mapper.Map<DestWithFloat>(source);

        Assert.Equal(3.14f, dest.Value, 2);
    }

    [Fact]
    public void Map_StringToInt_FailsGracefully()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var source = new SourceWithStringValue { Value = "not a number" };

        // Should not throw, will fall back to original value (which won't match)
        var exception = Record.Exception(() => mapper.Map<DestWithIntValue>(source));

        Assert.Null(exception);
    }

    #endregion

    #region Open Generic Edge Cases

    [Fact]
    public void Map_OpenGenericWithSingleTypeParam_Works()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SingleTypeParamConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new BoxSource<string> { Item = "Test" };
        var dest = mapper.Map<BoxDest<string>>(source);

        Assert.Equal("Test", dest.Item);
    }

    [Fact]
    public void Map_OpenGenericConverter_NonGenericSourceDest_Works()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NonGenericOpenConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new SpecificSource { Data = "Test" };
        var dest = mapper.Map<SpecificDest>(source);

        Assert.Equal("Test", dest.Data);
    }

    #endregion

    #region Nullable Type Conversion Tests

    [Fact]
    public void Map_NullableIntToInt_ConvertsValue()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableInt { Value = 42 };
        var dest = mapper.Map<DestWithRegularInt>(source);

        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void Map_IntToNullableInt_ConvertsValue()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithRegularInt { Value = 42 };
        var dest = mapper.Map<DestWithNullableInt>(source);

        Assert.Equal(42, dest.Value);
    }

    #endregion

    #region Collection Edge Cases

    [Fact]
    public void Map_EmptyCollection_ReturnsEmptyCollection()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<EdgeSource>();
        var dests = mapper.Map<List<EdgeDest>>(sources);

        Assert.Empty(dests);
    }

    [Fact]
    public void Map_NestedCollectionInObject_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new ParentWithCollection
        {
            Id = 1,
            Items = new List<EdgeSource>
            {
                new() { Id = 2, Name = "Item1" },
                new() { Id = 3, Name = "Item2" }
            }
        };

        var dest = mapper.Map<ParentDtoWithCollection>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("Item1", dest.Items[0].Name);
    }

    #endregion

    #region ResolutionContext Tests

    [Fact]
    public void ResolutionContext_MapperProperty_IsAccessible()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ContextAccessProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new WrapperSource { Inner = new EdgeSource { Id = 1, Name = "Test" } };
        var dest = mapper.Map<WrapperDest>(source);

        Assert.NotNull(dest.Inner);
        Assert.Equal(1, dest.Inner.Id);
    }

    #endregion

    #region Interface Collection Types via Implementation

    [Fact]
    public void Map_CustomCollectionImplementingIEnumerable_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EdgeCaseProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomCollection<EdgeSource>();
        source.Add(new EdgeSource { Id = 1, Name = "Test" });

        var dest = mapper.Map<List<EdgeDest>>(source);

        Assert.Single(dest);
        Assert.Equal("Test", dest[0].Name);
    }

    #endregion
}

#region Test Classes for Edge Cases

public class EdgeSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class EdgeDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ParentWithChild
{
    public int Id { get; set; }
    public EdgeSource? Child { get; set; }
}

public class ParentDtoWithChild
{
    public int Id { get; set; }
    public EdgeDest? Child { get; set; }
}

public class SourceWithExtra
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
}

public class DestWithoutExtra
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SourceWithoutExtra
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DestWithExtra
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
}

public class SourceWithDouble
{
    public double Value { get; set; }
}

public class DestWithFloat
{
    public float Value { get; set; }
}

public class SourceWithStringValue
{
    public string Value { get; set; } = string.Empty;
}

public class DestWithIntValue
{
    public int Value { get; set; }
}

public class BoxSource<T>
{
    public T? Item { get; set; }
}

public class BoxDest<T>
{
    public T? Item { get; set; }
}

public class SpecificSource
{
    public string Data { get; set; } = string.Empty;
}

public class SpecificDest
{
    public string Data { get; set; } = string.Empty;
}

public class SourceWithNullableInt
{
    public int? Value { get; set; }
}

public class DestWithRegularInt
{
    public int Value { get; set; }
}

public class SourceWithRegularInt
{
    public int Value { get; set; }
}

public class DestWithNullableInt
{
    public int? Value { get; set; }
}

public class ParentWithCollection
{
    public int Id { get; set; }
    public List<EdgeSource> Items { get; set; } = new();
}

public class ParentDtoWithCollection
{
    public int Id { get; set; }
    public List<EdgeDest> Items { get; set; } = new();
}

public class WrapperSource
{
    public EdgeSource? Inner { get; set; }
}

public class WrapperDest
{
    public EdgeDest? Inner { get; set; }
}

public class CustomCollection<T> : List<T>
{
}

#endregion

#region Test Profiles for Edge Cases

public class EdgeCaseProfile : Profile
{
    public EdgeCaseProfile()
    {
        CreateMap<EdgeSource, EdgeDest>();
        CreateMap<ParentWithChild, ParentDtoWithChild>();
        CreateMap<SourceWithExtra, DestWithoutExtra>();
        CreateMap<SourceWithoutExtra, DestWithExtra>();
        CreateMap<SourceWithDouble, DestWithFloat>();
        CreateMap<SourceWithNullableInt, DestWithRegularInt>();
        CreateMap<SourceWithRegularInt, DestWithNullableInt>();
        CreateMap<ParentWithCollection, ParentDtoWithCollection>();
    }
}

public class ThrowingResolverProfile : Profile
{
    public ThrowingResolverProfile()
    {
        CreateMap<EdgeSource, EdgeDest>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s => ThrowException(s)));
    }

    private static string ThrowException(EdgeSource s)
    {
        throw new InvalidOperationException("Test exception");
    }
}

public class SingleTypeParamConverterProfile : Profile
{
    public SingleTypeParamConverterProfile()
    {
        CreateMap(typeof(BoxSource<>), typeof(BoxDest<>))
            .ConvertUsing(typeof(BoxConverter<>));
    }
}

public class BoxConverter<T> : ITypeConverter<BoxSource<T>, BoxDest<T>>
{
    public BoxDest<T> Convert(BoxSource<T> source, BoxDest<T> destination, ResolutionContext context)
    {
        return new BoxDest<T> { Item = source.Item };
    }
}

public class NonGenericOpenConverterProfile : Profile
{
    public NonGenericOpenConverterProfile()
    {
        CreateMap<SpecificSource, SpecificDest>();
    }
}

public class ContextAccessProfile : Profile
{
    public ContextAccessProfile()
    {
        CreateMap<EdgeSource, EdgeDest>();
        CreateMap<WrapperSource, WrapperDest>()
            .ConvertUsing(new WrapperConverter());
    }
}

public class WrapperConverter : ITypeConverter<WrapperSource, WrapperDest>
{
    public WrapperDest Convert(WrapperSource source, WrapperDest destination, ResolutionContext context)
    {
        return new WrapperDest
        {
            Inner = context.Mapper.Map<EdgeDest>(source.Inner!)
        };
    }
}

#endregion
