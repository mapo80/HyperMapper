using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 Dictionaries.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class DictionaryMappingPortedTests
{
    #region Basic Dictionary Mapping Tests

    [Fact]
    public void Should_map_dictionary_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictionaryProfile>());
        var mapper = config.CreateMapper();

        var source = new DictSource
        {
            Data = new Dictionary<string, int>
            {
                { "key1", 1 },
                { "key2", 2 }
            }
        };

        var dest = mapper.Map<DictDest>(source);

        Assert.Equal(2, dest.Data.Count);
        Assert.Equal(1, dest.Data["key1"]);
        Assert.Equal(2, dest.Data["key2"]);
    }

    [Fact]
    public void Should_map_empty_dictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictionaryProfile>());
        var mapper = config.CreateMapper();

        var source = new DictSource { Data = new Dictionary<string, int>() };

        var dest = mapper.Map<DictDest>(source);

        Assert.NotNull(dest.Data);
        Assert.Empty(dest.Data);
    }

    [Fact]
    public void Should_map_null_dictionary_to_empty()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictionaryProfile>());
        var mapper = config.CreateMapper();

        var source = new DictSource { Data = null };

        var dest = mapper.Map<DictDest>(source);

        Assert.NotNull(dest.Data);
        Assert.Empty(dest.Data);
    }

    #endregion

    #region IDictionary Interface Mapping Tests

    [Fact]
    public void Should_map_to_IDictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IDictionaryProfile>());
        var mapper = config.CreateMapper();

        var source = new ConcreteDictSource
        {
            Data = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            }
        };

        var dest = mapper.Map<IDictDest>(source);

        Assert.IsType<Dictionary<string, string>>(dest.Data);
        Assert.Equal(2, dest.Data.Count);
    }

    #endregion

    #region Dictionary With Complex Values Tests

    [Fact]
    public void Should_map_dictionary_with_complex_values()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexDictProfile>());
        var mapper = config.CreateMapper();

        var source = new ComplexDictSource
        {
            Items = new Dictionary<int, DictItemSource>
            {
                { 1, new DictItemSource { Name = "Item1", Value = 10 } },
                { 2, new DictItemSource { Name = "Item2", Value = 20 } }
            }
        };

        var dest = mapper.Map<ComplexDictDest>(source);

        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("Item1", dest.Items[1].Name);
        Assert.Equal(20, dest.Items[2].Value);
    }

    #endregion

    #region Dictionary Key Type Conversion Tests

    [Fact]
    public void Should_map_dictionary_with_different_key_types()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<KeyConversionDictProfile>());
        var mapper = config.CreateMapper();

        var source = new StringKeyDictSource
        {
            Data = new Dictionary<string, int>
            {
                { "1", 100 },
                { "2", 200 }
            }
        };

        var dest = mapper.Map<IntKeyDictDest>(source);

        Assert.Equal(2, dest.Data.Count);
        Assert.Equal(100, dest.Data[1]);
        Assert.Equal(200, dest.Data[2]);
    }

    #endregion

    #region Dictionary To List Mapping Tests

    [Fact]
    public void Should_map_dictionary_values_to_list()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictToListProfile>());
        var mapper = config.CreateMapper();

        var source = new DictValuesSource
        {
            Items = new Dictionary<int, string>
            {
                { 1, "A" },
                { 2, "B" },
                { 3, "C" }
            }
        };

        var dest = mapper.Map<ListDest>(source);

        Assert.Equal(3, dest.Items.Count);
        Assert.Contains("A", dest.Items);
        Assert.Contains("B", dest.Items);
        Assert.Contains("C", dest.Items);
    }

    #endregion

    #region Nested Dictionary Mapping Tests

    [Fact]
    public void Should_map_nested_dictionaries()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedDictProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedDictSource
        {
            Categories = new Dictionary<string, Dictionary<string, int>>
            {
                { "cat1", new Dictionary<string, int> { { "item1", 1 }, { "item2", 2 } } },
                { "cat2", new Dictionary<string, int> { { "item3", 3 } } }
            }
        };

        var dest = mapper.Map<NestedDictDest>(source);

        Assert.Equal(2, dest.Categories.Count);
        Assert.Equal(2, dest.Categories["cat1"].Count);
        Assert.Single(dest.Categories["cat2"]);
    }

    #endregion

    #region IReadOnlyDictionary Mapping Tests

    [Fact]
    public void Should_map_to_IReadOnlyDictionary()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlyDictProfile>());
        var mapper = config.CreateMapper();

        var source = new WritableDictSource
        {
            Data = new Dictionary<string, int> { { "key", 42 } }
        };

        var dest = mapper.Map<ReadOnlyDictDest>(source);

        Assert.NotNull(dest.Data);
        Assert.Equal(42, dest.Data["key"]);
    }

    #endregion

    #region Object with Dictionary Property Tests

    [Fact]
    public void Should_map_object_with_dictionary_and_other_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MixedDictProfile>());
        var mapper = config.CreateMapper();

        var source = new MixedDictSource
        {
            Id = 1,
            Name = "Test",
            Attributes = new Dictionary<string, string>
            {
                { "color", "red" },
                { "size", "large" }
            }
        };

        var dest = mapper.Map<MixedDictDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
        Assert.Equal(2, dest.Attributes.Count);
        Assert.Equal("red", dest.Attributes["color"]);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Dictionary
public class DictSource
{
    public Dictionary<string, int>? Data { get; set; }
}

public class DictDest
{
    public Dictionary<string, int> Data { get; set; } = new();
}

public class DictionaryProfile : Profile
{
    public DictionaryProfile()
    {
        CreateMap<DictSource, DictDest>();
    }
}

// IDictionary Interface
public class ConcreteDictSource
{
    public Dictionary<string, string> Data { get; set; } = new();
}

public class IDictDest
{
    public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
}

public class IDictionaryProfile : Profile
{
    public IDictionaryProfile()
    {
        CreateMap<ConcreteDictSource, IDictDest>();
    }
}

// Complex Dictionary Values
public class DictItemSource
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DictItemDest
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class ComplexDictSource
{
    public Dictionary<int, DictItemSource> Items { get; set; } = new();
}

public class ComplexDictDest
{
    public Dictionary<int, DictItemDest> Items { get; set; } = new();
}

public class ComplexDictProfile : Profile
{
    public ComplexDictProfile()
    {
        CreateMap<DictItemSource, DictItemDest>();
        CreateMap<ComplexDictSource, ComplexDictDest>();
    }
}

// Key Type Conversion
public class StringKeyDictSource
{
    public Dictionary<string, int> Data { get; set; } = new();
}

public class IntKeyDictDest
{
    public Dictionary<int, int> Data { get; set; } = new();
}

public class KeyConversionDictProfile : Profile
{
    public KeyConversionDictProfile()
    {
        CreateMap<StringKeyDictSource, IntKeyDictDest>()
            .ForMember(d => d.Data, opt => opt.MapFrom(s =>
                s.Data.ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value)));
    }
}

// Dictionary To List
public class DictValuesSource
{
    public Dictionary<int, string> Items { get; set; } = new();
}

public class ListDest
{
    public List<string> Items { get; set; } = new();
}

public class DictToListProfile : Profile
{
    public DictToListProfile()
    {
        CreateMap<DictValuesSource, ListDest>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items.Values.ToList()));
    }
}

// Nested Dictionary
public class NestedDictSource
{
    public Dictionary<string, Dictionary<string, int>> Categories { get; set; } = new();
}

public class NestedDictDest
{
    public Dictionary<string, Dictionary<string, int>> Categories { get; set; } = new();
}

public class NestedDictProfile : Profile
{
    public NestedDictProfile()
    {
        CreateMap<NestedDictSource, NestedDictDest>();
    }
}

// IReadOnlyDictionary
public class WritableDictSource
{
    public Dictionary<string, int> Data { get; set; } = new();
}

public class ReadOnlyDictDest
{
    public IReadOnlyDictionary<string, int> Data { get; set; } = new Dictionary<string, int>();
}

public class ReadOnlyDictProfile : Profile
{
    public ReadOnlyDictProfile()
    {
        CreateMap<WritableDictSource, ReadOnlyDictDest>();
    }
}

// Mixed Dictionary and Properties
public class MixedDictSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class MixedDictDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class MixedDictProfile : Profile
{
    public MixedDictProfile()
    {
        CreateMap<MixedDictSource, MixedDictDest>();
    }
}

#endregion
