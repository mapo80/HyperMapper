using System.Collections.Specialized;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for StringDictionary mapping ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests/Mappers
/// License: MIT
///
/// Note: HyperMapper handles StringDictionary via custom converters.
/// </summary>
public class StringDictionaryPortedTests
{
    #region StringDictionary to Dictionary Tests

    [Fact]
    public void Should_map_StringDictionary_to_Dictionary_with_converter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringDictToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringDict
        {
            Data = new StringDictionary
            {
                { "name", "John" },
                { "city", "NYC" }
            }
        };

        var dest = mapper.Map<DestWithGenericDict>(source);

        Assert.Equal(2, dest.Data.Count);
        Assert.Equal("John", dest.Data["name"]);
        Assert.Equal("NYC", dest.Data["city"]);
    }

    [Fact]
    public void Should_handle_empty_StringDictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringDictToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringDict
        {
            Data = new StringDictionary()
        };

        var dest = mapper.Map<DestWithGenericDict>(source);

        Assert.NotNull(dest.Data);
        Assert.Empty(dest.Data);
    }

    [Fact]
    public void Should_handle_null_StringDictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringDictToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringDict
        {
            Data = null!
        };

        var dest = mapper.Map<DestWithGenericDict>(source);

        Assert.NotNull(dest.Data);
        Assert.Empty(dest.Data);
    }

    #endregion

    #region Dictionary to StringDictionary Tests

    [Fact]
    public void Should_map_Dictionary_to_StringDictionary_with_converter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictToStringDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithGenericDict
        {
            Data = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        var dest = mapper.Map<DestWithStringDict>(source);

        Assert.NotNull(dest.Data);
        Assert.Equal("value1", dest.Data["key1"]);
        Assert.Equal("value2", dest.Data["key2"]);
    }

    #endregion

    #region StringDictionary Case Sensitivity Tests

    [Fact]
    public void StringDictionary_should_be_case_insensitive()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringDictToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringDict
        {
            Data = new StringDictionary
            {
                { "Name", "John" }
            }
        };

        var dest = mapper.Map<DestWithGenericDict>(source);

        // StringDictionary converts keys to lowercase
        Assert.True(dest.Data.ContainsKey("name"));
        Assert.Equal("John", dest.Data["name"]);
    }

    #endregion

    #region Complex Object with StringDictionary Tests

    [Fact]
    public void Should_map_complex_object_with_StringDictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexStringDictProfile>());
        var mapper = config.CreateMapper();

        var source = new ComplexSourceWithStringDict
        {
            Id = 42,
            Title = "Test",
            Metadata = new StringDictionary
            {
                { "author", "Jane" },
                { "version", "1.0" }
            }
        };

        var dest = mapper.Map<ComplexDestWithGenericDict>(source);

        Assert.Equal(42, dest.Id);
        Assert.Equal("Test", dest.Title);
        Assert.Equal("Jane", dest.Metadata["author"]);
        Assert.Equal("1.0", dest.Metadata["version"]);
    }

    #endregion

    #region Collection of StringDictionary Tests

    [Fact]
    public void Should_map_list_of_objects_with_StringDictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringDictListProfile>());
        var mapper = config.CreateMapper();

        var source = new List<SourceWithStringDict>
        {
            new() { Data = new StringDictionary { { "a", "1" } } },
            new() { Data = new StringDictionary { { "b", "2" } } }
        };

        var dest = mapper.Map<List<DestWithGenericDict>>(source);

        Assert.Equal(2, dest.Count);
        Assert.Equal("1", dest[0].Data["a"]);
        Assert.Equal("2", dest[1].Data["b"]);
    }

    #endregion
}

#region Test Classes and Profiles

// StringDict to Dict
public class SourceWithStringDict
{
    public StringDictionary? Data { get; set; }
}

public class DestWithGenericDict
{
    public Dictionary<string, string> Data { get; set; } = new();
}

public class StringDictToDictConverter : ITypeConverter<SourceWithStringDict, DestWithGenericDict>
{
    public DestWithGenericDict Convert(SourceWithStringDict source, DestWithGenericDict destination, ResolutionContext context)
    {
        var result = new DestWithGenericDict();
        if (source.Data != null)
        {
            foreach (System.Collections.DictionaryEntry entry in source.Data)
            {
                if (entry.Key is string key)
                {
                    result.Data[key] = entry.Value?.ToString() ?? string.Empty;
                }
            }
        }
        return result;
    }
}

public class StringDictToDictProfile : Profile
{
    public StringDictToDictProfile()
    {
        CreateMap<SourceWithStringDict, DestWithGenericDict>()
            .ConvertUsing<StringDictToDictConverter>();
    }
}

// Dict to StringDict
public class SourceWithGenericDict
{
    public Dictionary<string, string> Data { get; set; } = new();
}

public class DestWithStringDict
{
    public StringDictionary? Data { get; set; }
}

public class DictToStringDictConverter : ITypeConverter<SourceWithGenericDict, DestWithStringDict>
{
    public DestWithStringDict Convert(SourceWithGenericDict source, DestWithStringDict destination, ResolutionContext context)
    {
        var stringDict = new StringDictionary();
        foreach (var kvp in source.Data)
        {
            stringDict.Add(kvp.Key, kvp.Value);
        }
        return new DestWithStringDict { Data = stringDict };
    }
}

public class DictToStringDictProfile : Profile
{
    public DictToStringDictProfile()
    {
        CreateMap<SourceWithGenericDict, DestWithStringDict>()
            .ConvertUsing<DictToStringDictConverter>();
    }
}

// Complex with StringDict
public class ComplexSourceWithStringDict
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public StringDictionary? Metadata { get; set; }
}

public class ComplexDestWithGenericDict
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class ComplexStringDictProfile : Profile
{
    public ComplexStringDictProfile()
    {
        CreateMap<ComplexSourceWithStringDict, ComplexDestWithGenericDict>()
            .ForMember(d => d.Metadata, opt => opt.MapFrom(s => ConvertStringDictToDict(s.Metadata)));
    }

    private static Dictionary<string, string> ConvertStringDictToDict(StringDictionary? sd)
    {
        var dict = new Dictionary<string, string>();
        if (sd != null)
        {
            foreach (System.Collections.DictionaryEntry entry in sd)
            {
                if (entry.Key is string key)
                {
                    dict[key] = entry.Value?.ToString() ?? string.Empty;
                }
            }
        }
        return dict;
    }
}

// List of StringDict
public class StringDictListProfile : Profile
{
    public StringDictListProfile()
    {
        CreateMap<SourceWithStringDict, DestWithGenericDict>()
            .ConvertUsing<StringDictToDictConverter>();
    }
}

#endregion
