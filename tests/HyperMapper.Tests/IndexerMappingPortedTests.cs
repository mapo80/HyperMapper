using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for indexer property mapping ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
///
/// Note: HyperMapper does not support indexer mapping directly.
/// These tests verify that regular property mapping works alongside classes with indexers.
/// </summary>
public class IndexerMappingPortedTests
{
    #region Basic Indexer Tests

    [Fact]
    public void Should_map_class_with_indexer_ignoring_indexer()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IndexerProfile>());
        var mapper = config.CreateMapper();

        var source = new IndexerSource
        {
            Name = "Test",
            Value = 42
        };

        var dest = mapper.Map<IndexerDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void Should_map_source_with_indexer_to_flat_dest()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IndexerToFlatProfile>());
        var mapper = config.CreateMapper();

        var source = new IndexerSourceWithData
        {
            Id = 1,
            Description = "Item"
        };
        source["key1"] = "value1";

        var dest = mapper.Map<IndexerFlatDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Item", dest.Description);
    }

    #endregion

    #region Collection with Indexer Tests

    [Fact]
    public void Should_map_list_of_objects_with_indexers()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IndexerProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<IndexerSource>
        {
            new() { Name = "First", Value = 1 },
            new() { Name = "Second", Value = 2 }
        };

        var dests = mapper.Map<List<IndexerDest>>(sources);

        Assert.Equal(2, dests.Count);
        Assert.Equal("First", dests[0].Name);
        Assert.Equal("Second", dests[1].Name);
    }

    #endregion

    #region Nested Object with Indexer Tests

    [Fact]
    public void Should_map_nested_object_with_indexer()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedIndexerProfile>());
        var mapper = config.CreateMapper();

        var source = new ParentWithIndexerChild
        {
            ParentName = "Parent",
            Child = new IndexerSource { Name = "Child", Value = 10 }
        };

        var dest = mapper.Map<ParentWithIndexerChildDest>(source);

        Assert.Equal("Parent", dest.ParentName);
        Assert.NotNull(dest.Child);
        Assert.Equal("Child", dest.Child.Name);
        Assert.Equal(10, dest.Child.Value);
    }

    #endregion

    #region Dictionary-like Indexer Tests

    [Fact]
    public void Should_map_object_with_dictionary_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DictionaryPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDictProperty
        {
            Id = 1,
            Properties = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        var dest = mapper.Map<DestWithDictProperty>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(2, dest.Properties.Count);
        Assert.Equal("value1", dest.Properties["key1"]);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Indexer Source
public class IndexerSource
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }

    private readonly Dictionary<string, object> _data = new();

    public object this[string key]
    {
        get => _data.TryGetValue(key, out var val) ? val : null!;
        set => _data[key] = value;
    }
}

public class IndexerDest
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class IndexerProfile : Profile
{
    public IndexerProfile()
    {
        CreateMap<IndexerSource, IndexerDest>();
    }
}

// Indexer Source with Data
public class IndexerSourceWithData
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    private readonly Dictionary<string, string> _items = new();

    public string this[string key]
    {
        get => _items.TryGetValue(key, out var val) ? val : string.Empty;
        set => _items[key] = value;
    }
}

public class IndexerFlatDest
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class IndexerToFlatProfile : Profile
{
    public IndexerToFlatProfile()
    {
        CreateMap<IndexerSourceWithData, IndexerFlatDest>();
    }
}

// Nested Indexer
public class ParentWithIndexerChild
{
    public string ParentName { get; set; } = string.Empty;
    public IndexerSource? Child { get; set; }
}

public class ParentWithIndexerChildDest
{
    public string ParentName { get; set; } = string.Empty;
    public IndexerDest? Child { get; set; }
}

public class NestedIndexerProfile : Profile
{
    public NestedIndexerProfile()
    {
        CreateMap<IndexerSource, IndexerDest>();
        CreateMap<ParentWithIndexerChild, ParentWithIndexerChildDest>();
    }
}

// Dictionary Property
public class SourceWithDictProperty
{
    public int Id { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class DestWithDictProperty
{
    public int Id { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class DictionaryPropertyProfile : Profile
{
    public DictionaryPropertyProfile()
    {
        CreateMap<SourceWithDictProperty, DestWithDictProperty>();
    }
}

#endregion
