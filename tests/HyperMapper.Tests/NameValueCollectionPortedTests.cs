using System.Collections.Specialized;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for NameValueCollection mapping ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests/Mappers
/// License: MIT
///
/// Note: HyperMapper handles NameValueCollection via custom converters.
/// </summary>
public class NameValueCollectionPortedTests
{
    #region NameValueCollection to Dictionary Tests

    [Fact]
    public void Should_map_NameValueCollection_to_Dictionary_with_converter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NvcToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNvc
        {
            Values = new NameValueCollection
            {
                { "key1", "value1" },
                { "key2", "value2" }
            }
        };

        var dest = mapper.Map<DestWithDict>(source);

        Assert.Equal(2, dest.Values.Count);
        Assert.Equal("value1", dest.Values["key1"]);
        Assert.Equal("value2", dest.Values["key2"]);
    }

    [Fact]
    public void Should_handle_empty_NameValueCollection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NvcToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNvc
        {
            Values = new NameValueCollection()
        };

        var dest = mapper.Map<DestWithDict>(source);

        Assert.NotNull(dest.Values);
        Assert.Empty(dest.Values);
    }

    [Fact]
    public void Should_handle_null_NameValueCollection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NvcToDictProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNvc
        {
            Values = null!
        };

        var dest = mapper.Map<DestWithDict>(source);

        Assert.NotNull(dest.Values);
        Assert.Empty(dest.Values);
    }

    #endregion

    #region Dictionary to NameValueCollection Tests

    [Fact]
    public void Should_map_Dictionary_to_NameValueCollection_with_converter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictToNvcProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDict
        {
            Values = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        var dest = mapper.Map<DestWithNvc>(source);

        Assert.NotNull(dest.Values);
        Assert.Equal("value1", dest.Values["key1"]);
        Assert.Equal("value2", dest.Values["key2"]);
    }

    #endregion

    #region NameValueCollection with Multiple Values Tests

    [Fact]
    public void Should_handle_multiple_values_for_same_key()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NvcMultiValueProfile>());
        var mapper = config.CreateMapper();

        var nvc = new NameValueCollection();
        nvc.Add("tags", "tag1");
        nvc.Add("tags", "tag2");
        nvc.Add("tags", "tag3");

        var source = new SourceWithNvc { Values = nvc };
        var dest = mapper.Map<DestWithListDict>(source);

        Assert.True(dest.Values.ContainsKey("tags"));
        Assert.Equal(3, dest.Values["tags"].Count);
        Assert.Contains("tag1", dest.Values["tags"]);
        Assert.Contains("tag2", dest.Values["tags"]);
    }

    #endregion

    #region Complex Object with NameValueCollection Tests

    [Fact]
    public void Should_map_complex_object_with_NameValueCollection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexNvcProfile>());
        var mapper = config.CreateMapper();

        var source = new ComplexSourceWithNvc
        {
            Id = 1,
            Name = "Test",
            Headers = new NameValueCollection
            {
                { "Content-Type", "application/json" },
                { "Authorization", "Bearer token" }
            }
        };

        var dest = mapper.Map<ComplexDestWithDict>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
        Assert.Equal("application/json", dest.Headers["Content-Type"]);
    }

    #endregion
}

#region Test Classes and Profiles

// NVC to Dict
public class SourceWithNvc
{
    public NameValueCollection? Values { get; set; }
}

public class DestWithDict
{
    public Dictionary<string, string> Values { get; set; } = new();
}

public class NvcToDictConverter : ITypeConverter<SourceWithNvc, DestWithDict>
{
    public DestWithDict Convert(SourceWithNvc source, DestWithDict destination, ResolutionContext context)
    {
        var result = new DestWithDict();
        if (source.Values != null)
        {
            foreach (string? key in source.Values.AllKeys)
            {
                if (key != null)
                {
                    result.Values[key] = source.Values[key] ?? string.Empty;
                }
            }
        }
        return result;
    }
}

public class NvcToDictProfile : Profile
{
    public NvcToDictProfile()
    {
        CreateMap<SourceWithNvc, DestWithDict>()
            .ConvertUsing<NvcToDictConverter>();
    }
}

// Dict to NVC
public class SourceWithDict
{
    public Dictionary<string, string> Values { get; set; } = new();
}

public class DestWithNvc
{
    public NameValueCollection? Values { get; set; }
}

public class DictToNvcConverter : ITypeConverter<SourceWithDict, DestWithNvc>
{
    public DestWithNvc Convert(SourceWithDict source, DestWithNvc destination, ResolutionContext context)
    {
        var nvc = new NameValueCollection();
        foreach (var kvp in source.Values)
        {
            nvc.Add(kvp.Key, kvp.Value);
        }
        return new DestWithNvc { Values = nvc };
    }
}

public class DictToNvcProfile : Profile
{
    public DictToNvcProfile()
    {
        CreateMap<SourceWithDict, DestWithNvc>()
            .ConvertUsing<DictToNvcConverter>();
    }
}

// Multi-value NVC
public class DestWithListDict
{
    public Dictionary<string, List<string>> Values { get; set; } = new();
}

public class NvcMultiValueConverter : ITypeConverter<SourceWithNvc, DestWithListDict>
{
    public DestWithListDict Convert(SourceWithNvc source, DestWithListDict destination, ResolutionContext context)
    {
        var result = new DestWithListDict();
        if (source.Values != null)
        {
            foreach (string? key in source.Values.AllKeys)
            {
                if (key != null)
                {
                    var values = source.Values.GetValues(key);
                    result.Values[key] = values?.ToList() ?? new List<string>();
                }
            }
        }
        return result;
    }
}

public class NvcMultiValueProfile : Profile
{
    public NvcMultiValueProfile()
    {
        CreateMap<SourceWithNvc, DestWithListDict>()
            .ConvertUsing<NvcMultiValueConverter>();
    }
}

// Complex with NVC
public class ComplexSourceWithNvc
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NameValueCollection? Headers { get; set; }
}

public class ComplexDestWithDict
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class ComplexNvcProfile : Profile
{
    public ComplexNvcProfile()
    {
        CreateMap<ComplexSourceWithNvc, ComplexDestWithDict>()
            .ForMember(d => d.Headers, opt => opt.MapFrom(s => ConvertNvcToDict(s.Headers)));
    }

    private static Dictionary<string, string> ConvertNvcToDict(NameValueCollection? nvc)
    {
        var dict = new Dictionary<string, string>();
        if (nvc != null)
        {
            foreach (string? key in nvc.AllKeys)
            {
                if (key != null)
                {
                    dict[key] = nvc[key] ?? string.Empty;
                }
            }
        }
        return dict;
    }
}

#endregion
