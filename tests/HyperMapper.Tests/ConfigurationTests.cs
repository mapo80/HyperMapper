using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for MapperConfiguration, Profile, and related configuration classes.
/// </summary>
public class ConfigurationTests
{
    #region MapperConfiguration Tests

    [Fact]
    public void MapperConfiguration_WithEmptyConfig_CreatesMapper()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        Assert.NotNull(mapper);
    }

    [Fact]
    public void MapperConfiguration_AddProfileInstance_Works()
    {
        var profile = new ConfigTestProfile();
        var config = new MapperConfiguration(cfg => cfg.AddProfile(profile));
        var mapper = config.CreateMapper();

        var source = new ConfigSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<ConfigDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void MapperConfiguration_AddProfileGeneric_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConfigTestProfile>());
        var mapper = config.CreateMapper();

        var source = new ConfigSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<ConfigDest>(source);

        Assert.Equal(1, dest.Id);
    }

    [Fact]
    public void MapperConfiguration_AssertConfigurationIsValid_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConfigTestProfile>());

        var exception = Record.Exception(() => config.AssertConfigurationIsValid());

        Assert.Null(exception);
    }

    [Fact]
    public void MapperConfiguration_MultipleProfiles_AllMappingsWork()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ConfigTestProfile>();
            cfg.AddProfile<SecondTestProfile>();
        });
        var mapper = config.CreateMapper();

        var source1 = new ConfigSource { Id = 1, Name = "Test1" };
        var dest1 = mapper.Map<ConfigDest>(source1);

        var source2 = new SecondSource { Value = "Test2" };
        var dest2 = mapper.Map<SecondDest>(source2);

        Assert.Equal("Test1", dest1.Name);
        Assert.Equal("Test2", dest2.Value);
    }

    #endregion

    #region Profile CreateMap Tests

    [Fact]
    public void Profile_CreateMap_ReturnsIMappingExpression()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FluentProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new FluentSource { A = 1, B = "test" };
        var dest = mapper.Map<FluentDest>(source);

        Assert.Equal(1, dest.A);
        Assert.Equal("test", dest.B);
    }

    [Fact]
    public void Profile_CreateMapOpenGeneric_Works()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OpenGenericProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new GenericSource<int> { Data = 42 };
        var dest = mapper.Map<GenericDest<int>>(source);

        Assert.Equal(42, dest.Data);
    }

    #endregion

    #region ForMember Tests

    [Fact]
    public void ForMember_ChainedCalls_Work()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ChainedForMemberProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ChainedSource { Value1 = "A", Value2 = "B", Value3 = "C" };
        var dest = mapper.Map<ChainedDest>(source);

        Assert.Equal("A", dest.Mapped1);
        Assert.Equal("B", dest.Mapped2);
        Assert.Null(dest.Ignored);
    }

    [Fact]
    public void ForMember_MapFromExpression_MapsNestedProperty()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NestedExpressionProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new SourceWithNested
        {
            Nested = new NestedObject { DeepValue = "Deep" }
        };
        var dest = mapper.Map<FlatDest>(source);

        Assert.Equal("Deep", dest.Value);
    }

    #endregion

    #region ConvertUsing Tests

    [Fact]
    public void ConvertUsing_GenericMethod_Works()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<GenericConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ConverterSource { Input = "hello" };
        var dest = mapper.Map<ConverterDest>(source);

        Assert.Equal("HELLO", dest.Output);
    }

    #endregion

    #region ReverseMap Tests

    [Fact]
    public void ReverseMap_CreatesReverseMappingWithChaining()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ReverseMapProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ReverseSource { Id = 1, Name = "Original" };
        var dest = mapper.Map<ReverseDest>(source);
        var backToSource = mapper.Map<ReverseSource>(dest);

        Assert.Equal(1, backToSource.Id);
        Assert.Equal("Original", backToSource.Name);
    }

    [Fact]
    public void ReverseMap_AllowsFurtherConfiguration()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ReverseMapWithConfigProfile>();
        });
        var mapper = config.CreateMapper();

        var dest = new ReverseDest { Id = 5, Name = "Test" };
        var source = mapper.Map<ReverseSource>(dest);

        Assert.Equal(5, source.Id);
    }

    #endregion
}

#region Test Classes for Configuration

public class ConfigSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ConfigDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SecondSource
{
    public string Value { get; set; } = string.Empty;
}

public class SecondDest
{
    public string Value { get; set; } = string.Empty;
}

public class FluentSource
{
    public int A { get; set; }
    public string B { get; set; } = string.Empty;
}

public class FluentDest
{
    public int A { get; set; }
    public string B { get; set; } = string.Empty;
}

public class GenericSource<T>
{
    public T? Data { get; set; }
}

public class GenericDest<T>
{
    public T? Data { get; set; }
}

public class ChainedSource
{
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
    public string Value3 { get; set; } = string.Empty;
}

public class ChainedDest
{
    public string Mapped1 { get; set; } = string.Empty;
    public string Mapped2 { get; set; } = string.Empty;
    public string? Ignored { get; set; }
}

public class NestedObject
{
    public string DeepValue { get; set; } = string.Empty;
}

public class SourceWithNested
{
    public NestedObject? Nested { get; set; }
}

public class FlatDest
{
    public string Value { get; set; } = string.Empty;
}

public class ConverterSource
{
    public string Input { get; set; } = string.Empty;
}

public class ConverterDest
{
    public string Output { get; set; } = string.Empty;
}

public class ReverseSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ReverseDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

#region Test Profiles for Configuration

public class ConfigTestProfile : Profile
{
    public ConfigTestProfile()
    {
        CreateMap<ConfigSource, ConfigDest>();
    }
}

public class SecondTestProfile : Profile
{
    public SecondTestProfile()
    {
        CreateMap<SecondSource, SecondDest>();
    }
}

public class FluentProfile : Profile
{
    public FluentProfile()
    {
        CreateMap<FluentSource, FluentDest>();
    }
}

public class OpenGenericProfile : Profile
{
    public OpenGenericProfile()
    {
        CreateMap(typeof(GenericSource<>), typeof(GenericDest<>));
    }
}

public class ChainedForMemberProfile : Profile
{
    public ChainedForMemberProfile()
    {
        CreateMap<ChainedSource, ChainedDest>()
            .ForMember(d => d.Mapped1, opt => opt.MapFrom(s => s.Value1))
            .ForMember(d => d.Mapped2, opt => opt.MapFrom(s => s.Value2))
            .ForMember(d => d.Ignored, opt => opt.Ignore());
    }
}

public class NestedExpressionProfile : Profile
{
    public NestedExpressionProfile()
    {
        CreateMap<SourceWithNested, FlatDest>()
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.Nested!.DeepValue));
    }
}

public class GenericConverterProfile : Profile
{
    public GenericConverterProfile()
    {
        CreateMap<ConverterSource, ConverterDest>()
            .ConvertUsing<UpperCaseConverter>();
    }
}

public class UpperCaseConverter : ITypeConverter<ConverterSource, ConverterDest>
{
    public ConverterDest Convert(ConverterSource source, ConverterDest destination, ResolutionContext context)
    {
        return new ConverterDest { Output = source.Input.ToUpperInvariant() };
    }
}

public class ReverseMapProfile : Profile
{
    public ReverseMapProfile()
    {
        CreateMap<ReverseSource, ReverseDest>().ReverseMap();
    }
}

public class ReverseMapWithConfigProfile : Profile
{
    public ReverseMapWithConfigProfile()
    {
        CreateMap<ReverseSource, ReverseDest>()
            .ReverseMap()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
    }
}

#endregion
