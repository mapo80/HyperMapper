using System.Collections.ObjectModel;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for ReadOnlyCollection mapping ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class ReadOnlyCollectionPortedTests
{
    #region Basic ReadOnlyCollection Tests

    [Fact]
    public void Should_map_list_to_IReadOnlyList()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlyListProfile>());
        var mapper = config.CreateMapper();

        var source = new ListSource
        {
            Items = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<ReadOnlyListDest>(source);

        Assert.Equal(3, dest.Items.Count);
        Assert.Equal(1, dest.Items[0]);
        Assert.Equal(2, dest.Items[1]);
        Assert.Equal(3, dest.Items[2]);
    }

    [Fact]
    public void Should_map_list_to_IReadOnlyCollection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlyCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new ListSource
        {
            Items = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<ReadOnlyCollectionDest>(source);

        Assert.Equal(3, dest.Items.Count);
        Assert.Contains(1, dest.Items);
        Assert.Contains(2, dest.Items);
        Assert.Contains(3, dest.Items);
    }

    #endregion

    #region ReadOnlyCollection with Complex Types

    [Fact]
    public void Should_map_list_of_complex_to_IReadOnlyList()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexReadOnlyProfile>());
        var mapper = config.CreateMapper();

        var source = new ComplexListSource
        {
            Items = new List<ROItemSource>
            {
                new() { Id = 1, Name = "First" },
                new() { Id = 2, Name = "Second" }
            }
        };

        var dest = mapper.Map<ComplexReadOnlyDest>(source);

        Assert.Equal(2, dest.Items.Count);
        Assert.Equal(1, dest.Items[0].Id);
        Assert.Equal("First", dest.Items[0].Name);
    }

    #endregion

    #region ReadOnlyCollection to List

    [Fact]
    public void Should_map_IReadOnlyList_to_List()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlyToListProfile>());
        var mapper = config.CreateMapper();

        var source = new ROReadOnlyListSource
        {
            Items = new List<int> { 1, 2, 3 }.AsReadOnly()
        };

        var dest = mapper.Map<ROListDest>(source);

        Assert.Equal(3, dest.Items.Count);
        Assert.IsType<List<int>>(dest.Items);
    }

    #endregion

    #region ReadOnlyCollection of Strings

    [Fact]
    public void Should_map_list_of_strings_to_IReadOnlyList()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringReadOnlyProfile>());
        var mapper = config.CreateMapper();

        var source = new StringListSource
        {
            Names = new List<string> { "Alice", "Bob", "Charlie" }
        };

        var dest = mapper.Map<StringReadOnlyDest>(source);

        Assert.Equal(3, dest.Names.Count);
        Assert.Equal("Alice", dest.Names[0]);
    }

    #endregion

    #region Empty ReadOnlyCollection

    [Fact]
    public void Should_map_empty_list_to_empty_IReadOnlyList()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlyListProfile>());
        var mapper = config.CreateMapper();

        var source = new ListSource
        {
            Items = new List<int>()
        };

        var dest = mapper.Map<ReadOnlyListDest>(source);

        Assert.Empty(dest.Items);
    }

    #endregion

    #region Nested ReadOnlyCollection

    [Fact]
    public void Should_map_nested_readonly_collections()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedReadOnlyProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedROSource
        {
            Groups = new List<ROGroupSource>
            {
                new()
                {
                    Name = "Group1",
                    Items = new List<int> { 1, 2 }
                },
                new()
                {
                    Name = "Group2",
                    Items = new List<int> { 3, 4 }
                }
            }
        };

        var dest = mapper.Map<NestedRODest>(source);

        Assert.Equal(2, dest.Groups.Count);
        Assert.Equal("Group1", dest.Groups[0].Name);
        Assert.Equal(2, dest.Groups[0].Items.Count);
    }

    #endregion

    #region IReadOnlySet Tests

    [Fact]
    public void Should_map_HashSet_to_IReadOnlySet()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ROReadOnlySetProfile>());
        var mapper = config.CreateMapper();

        var source = new ROHashSetSource
        {
            Values = new HashSet<int> { 1, 2, 3, 2, 1 } // Duplicates should be removed
        };

        var dest = mapper.Map<ROReadOnlySetDest>(source);

        Assert.Equal(3, dest.Values.Count);
        Assert.Contains(1, dest.Values);
        Assert.Contains(2, dest.Values);
        Assert.Contains(3, dest.Values);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic ReadOnlyList
public class ListSource
{
    public List<int> Items { get; set; } = new();
}

public class ReadOnlyListDest
{
    public IReadOnlyList<int> Items { get; set; } = new List<int>();
}

public class ReadOnlyListProfile : Profile
{
    public ReadOnlyListProfile()
    {
        CreateMap<ListSource, ReadOnlyListDest>();
    }
}

// ReadOnlyCollection
public class ReadOnlyCollectionDest
{
    public IReadOnlyCollection<int> Items { get; set; } = new List<int>();
}

public class ReadOnlyCollectionProfile : Profile
{
    public ReadOnlyCollectionProfile()
    {
        CreateMap<ListSource, ReadOnlyCollectionDest>();
    }
}

// Complex ReadOnly
public class ROItemSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ROItemDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ComplexListSource
{
    public List<ROItemSource> Items { get; set; } = new();
}

public class ComplexReadOnlyDest
{
    public IReadOnlyList<ROItemDest> Items { get; set; } = new List<ROItemDest>();
}

public class ComplexReadOnlyProfile : Profile
{
    public ComplexReadOnlyProfile()
    {
        CreateMap<ROItemSource, ROItemDest>();
        CreateMap<ComplexListSource, ComplexReadOnlyDest>();
    }
}

// ReadOnly to List
public class ROReadOnlyListSource
{
    public IReadOnlyList<int> Items { get; set; } = new List<int>();
}

public class ROListDest
{
    public List<int> Items { get; set; } = new();
}

public class ReadOnlyToListProfile : Profile
{
    public ReadOnlyToListProfile()
    {
        CreateMap<ROReadOnlyListSource, ROListDest>();
    }
}

// String ReadOnly
public class StringListSource
{
    public List<string> Names { get; set; } = new();
}

public class StringReadOnlyDest
{
    public IReadOnlyList<string> Names { get; set; } = new List<string>();
}

public class StringReadOnlyProfile : Profile
{
    public StringReadOnlyProfile()
    {
        CreateMap<StringListSource, StringReadOnlyDest>();
    }
}

// Nested ReadOnly
public class ROGroupSource
{
    public string Name { get; set; } = string.Empty;
    public List<int> Items { get; set; } = new();
}

public class ROGroupDest
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<int> Items { get; set; } = new List<int>();
}

public class NestedROSource
{
    public List<ROGroupSource> Groups { get; set; } = new();
}

public class NestedRODest
{
    public IReadOnlyList<ROGroupDest> Groups { get; set; } = new List<ROGroupDest>();
}

public class NestedReadOnlyProfile : Profile
{
    public NestedReadOnlyProfile()
    {
        CreateMap<ROGroupSource, ROGroupDest>();
        CreateMap<NestedROSource, NestedRODest>();
    }
}

// ReadOnlySet
public class ROHashSetSource
{
    public HashSet<int> Values { get; set; } = new();
}

public class ROReadOnlySetDest
{
    public IReadOnlySet<int> Values { get; set; } = new HashSet<int>();
}

public class ROReadOnlySetProfile : Profile
{
    public ROReadOnlySetProfile()
    {
        CreateMap<ROHashSetSource, ROReadOnlySetDest>();
    }
}

#endregion
