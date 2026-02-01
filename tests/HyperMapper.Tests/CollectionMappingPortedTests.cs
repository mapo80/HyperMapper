using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 ArraysAndLists.cs and CollectionMapping.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class CollectionMappingPortedTests
{
    #region Array Mapping Tests

    [Fact]
    public void Should_map_arrays()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new[]
        {
            new ArraySource { Value = 1 },
            new ArraySource { Value = 2 },
            new ArraySource { Value = 3 }
        };
        var dests = mapper.Map<ArrayDest[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(1, dests[0].Value);
        Assert.Equal(2, dests[1].Value);
        Assert.Equal(3, dests[2].Value);
    }

    [Fact]
    public void Should_map_array_to_list()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new[] { new ArraySource { Value = 1 }, new ArraySource { Value = 2 } };
        var dests = mapper.Map<List<ArrayDest>>(sources);

        Assert.Equal(2, dests.Count);
        Assert.Equal(1, dests[0].Value);
        Assert.Equal(2, dests[1].Value);
    }

    [Fact]
    public void Should_map_list_to_array()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<ArraySource> { new() { Value = 1 }, new() { Value = 2 } };
        var dests = mapper.Map<ArrayDest[]>(sources);

        Assert.Equal(2, dests.Length);
        Assert.Equal(1, dests[0].Value);
        Assert.Equal(2, dests[1].Value);
    }

    [Fact]
    public void Should_map_IEnumerable_to_array()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        IEnumerable<ArraySource> sources = new List<ArraySource> { new() { Value = 1 }, new() { Value = 2 } };
        var dests = mapper.Map<ArrayDest[]>(sources);

        Assert.Equal(2, dests.Length);
    }

    [Fact]
    public void Should_map_IEnumerable_to_list()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        IEnumerable<ArraySource> sources = new List<ArraySource> { new() { Value = 1 }, new() { Value = 2 } };
        var dests = mapper.Map<List<ArrayDest>>(sources);

        Assert.Equal(2, dests.Count);
    }

    #endregion

    #region Null Items in Collections Tests

    [Fact]
    public void Should_map_array_with_null_items()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new ArraySource?[] { new() { Value = 1 }, null, new() { Value = 3 } };
        var dests = mapper.Map<List<ArrayDest?>>(sources);

        Assert.Equal(3, dests.Count);
        Assert.Equal(1, dests[0]?.Value);
        Assert.Null(dests[1]);
        Assert.Equal(3, dests[2]?.Value);
    }

    #endregion

    #region Nested Collections Tests

    [Fact]
    public void Should_map_nested_collection_in_object()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new ParentWithArray
        {
            Id = 1,
            Children = new[]
            {
                new ChildSource { Name = "Child1" },
                new ChildSource { Name = "Child2" }
            }
        };

        var dest = mapper.Map<ParentWithList>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(2, dest.Children.Count);
        Assert.Equal("Child1", dest.Children[0].Name);
        Assert.Equal("Child2", dest.Children[1].Name);
    }

    [Fact]
    public void Should_map_list_to_IEnumerable()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new ParentWithList
        {
            Id = 1,
            Children = new List<ChildDest>
            {
                new() { Name = "Child1" },
                new() { Name = "Child2" }
            }
        };

        var dest = mapper.Map<ParentWithEnumerable>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(2, dest.Children.Count());
    }

    #endregion

    #region Empty Collection Tests

    [Fact]
    public void Should_map_empty_array()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = Array.Empty<ArraySource>();
        var dests = mapper.Map<ArrayDest[]>(sources);

        Assert.Empty(dests);
    }

    [Fact]
    public void Should_map_empty_list()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<ArraySource>();
        var dests = mapper.Map<List<ArrayDest>>(sources);

        Assert.Empty(dests);
    }

    #endregion

    #region IReadOnlyList and IReadOnlyCollection Tests

    [Fact]
    public void Should_map_to_IReadOnlyList()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<ArraySource> { new() { Value = 1 }, new() { Value = 2 } };
        var dests = mapper.Map<IReadOnlyList<ArrayDest>>(sources);

        Assert.Equal(2, dests.Count);
        Assert.Equal(1, dests[0].Value);
    }

    [Fact]
    public void Should_map_to_IReadOnlyCollection()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArrayProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<ArraySource> { new() { Value = 1 }, new() { Value = 2 } };
        var dests = mapper.Map<IReadOnlyCollection<ArrayDest>>(sources);

        Assert.Equal(2, dests.Count);
    }

    #endregion

    #region Primitive Type Collections Tests

    [Fact]
    public void Should_map_int_array()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new[] { 1, 2, 3, 4, 5 };
        var dests = mapper.Map<int[]>(sources);

        Assert.Equal(5, dests.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, dests);
    }

    [Fact]
    public void Should_map_string_list()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new List<string> { "a", "b", "c" };
        var dests = mapper.Map<List<string>>(sources);

        Assert.Equal(3, dests.Count);
        Assert.Equal(new[] { "a", "b", "c" }, dests);
    }

    #endregion

    #region Collection with Complex Type Conversion Tests

    [Fact]
    public void Should_map_collection_with_type_conversion()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionTypeConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new IntArrayContainer { Values = new[] { 1, 2, 3 } };
        var dest = mapper.Map<LongListContainer>(source);

        Assert.Equal(3, dest.Values.Count);
        Assert.Equal(new List<long> { 1, 2, 3 }, dest.Values);
    }

    #endregion
}

#region Test Classes and Profiles

// Array/List Source and Dest
public class ArraySource
{
    public int Value { get; set; }
}

public class ArrayDest
{
    public int Value { get; set; }
}

public class ArrayProfile : Profile
{
    public ArrayProfile()
    {
        CreateMap<ArraySource, ArrayDest>();
    }
}

// Nested Collections
public class ChildSource
{
    public string Name { get; set; } = string.Empty;
}

public class ChildDest
{
    public string Name { get; set; } = string.Empty;
}

public class ParentWithArray
{
    public int Id { get; set; }
    public ChildSource[] Children { get; set; } = Array.Empty<ChildSource>();
}

public class ParentWithList
{
    public int Id { get; set; }
    public List<ChildDest> Children { get; set; } = new();
}

public class ParentWithEnumerable
{
    public int Id { get; set; }
    public IEnumerable<ChildDest> Children { get; set; } = Enumerable.Empty<ChildDest>();
}

public class NestedCollectionProfile : Profile
{
    public NestedCollectionProfile()
    {
        CreateMap<ChildSource, ChildDest>();
        CreateMap<ChildDest, ChildSource>();
        CreateMap<ParentWithArray, ParentWithList>();
        CreateMap<ParentWithList, ParentWithEnumerable>();
    }
}

// Collection Type Conversion
public class IntArrayContainer
{
    public int[] Values { get; set; } = Array.Empty<int>();
}

public class LongListContainer
{
    public List<long> Values { get; set; } = new();
}

public class CollectionTypeConversionProfile : Profile
{
    public CollectionTypeConversionProfile()
    {
        CreateMap<IntArrayContainer, LongListContainer>()
            .ForMember(d => d.Values, opt => opt.MapFrom(s => s.Values.Select(v => (long)v).ToList()));
    }
}

#endregion
