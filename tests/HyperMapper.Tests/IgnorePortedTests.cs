using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 related to Ignore functionality
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class IgnorePortedTests
{
    #region Basic Ignore Tests

    [Fact]
    public void Should_ignore_single_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BasicIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new IgnoreBasicSource { Included = "Yes", Excluded = "No" };
        var dest = mapper.Map<IgnoreBasicDest>(source);

        Assert.Equal("Yes", dest.Included);
        Assert.Null(dest.Excluded);
    }

    [Fact]
    public void Should_ignore_multiple_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultiIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new MultiIgnoreSource
        {
            Keep = "Keep",
            Skip1 = "Skip1",
            Skip2 = "Skip2"
        };

        var dest = mapper.Map<MultiIgnoreDest>(source);

        Assert.Equal("Keep", dest.Keep);
        Assert.Null(dest.Skip1);
        Assert.Null(dest.Skip2);
    }

    #endregion

    #region Ignore with Existing Destination Tests

    [Fact]
    public void Should_preserve_ignored_property_on_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BasicIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new IgnoreBasicSource { Included = "New", Excluded = "ShouldNotChange" };
        var dest = new IgnoreBasicDest { Included = "Old", Excluded = "Preserved" };

        mapper.Map(source, dest);

        Assert.Equal("New", dest.Included);
        Assert.Equal("Preserved", dest.Excluded); // Preserved due to Ignore()
    }

    #endregion

    #region Ignore Nested Property Tests

    [Fact]
    public void Should_ignore_nested_object_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedIgnoreSource
        {
            Name = "Test",
            Nested = new NestedIgnoreInnerSource { Value = 42 }
        };

        var dest = mapper.Map<NestedIgnoreDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Null(dest.Nested); // Ignored
    }

    #endregion

    #region Ignore Collection Property Tests

    [Fact]
    public void Should_ignore_collection_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new CollectionIgnoreSource
        {
            Name = "Test",
            Items = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<CollectionIgnoreDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Null(dest.Items); // Ignored
    }

    [Fact]
    public void Should_preserve_ignored_collection_on_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new CollectionIgnoreSource
        {
            Name = "Test",
            Items = new List<int> { 1, 2, 3 }
        };

        var existingItems = new List<int> { 10, 20 };
        var dest = new CollectionIgnoreDest { Name = "Old", Items = existingItems };

        mapper.Map(source, dest);

        Assert.Equal("Test", dest.Name);
        Assert.Same(existingItems, dest.Items); // Preserved
        Assert.Equal(new[] { 10, 20 }, dest.Items);
    }

    #endregion

    #region Ignore with ForMember Combined Tests

    [Fact]
    public void Should_combine_ignore_with_mapfrom()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CombinedIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new CombinedIgnoreSource
        {
            First = "John",
            Last = "Doe",
            Secret = "Hidden"
        };

        var dest = mapper.Map<CombinedIgnoreDest>(source);

        Assert.Equal("John Doe", dest.FullName);
        Assert.Null(dest.Secret);
    }

    #endregion

    #region Ignore All Unmatched Properties Tests

    [Fact]
    public void Should_only_map_configured_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<SelectiveIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new SelectiveIgnoreSource
        {
            A = "A",
            B = "B",
            C = "C",
            D = "D"
        };

        var dest = mapper.Map<SelectiveIgnoreDest>(source);

        Assert.Equal("A", dest.A); // Mapped
        Assert.Null(dest.B); // Ignored
        Assert.Null(dest.C); // Ignored
        Assert.Null(dest.D); // Ignored
    }

    #endregion

    #region Ignore with ReverseMap Tests

    [Fact]
    public void Should_not_reverse_ignore_by_default()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IgnoreReverseProfile>());
        var mapper = config.CreateMapper();

        // Forward mapping ignores Excluded
        var source = new IgnoreReverseSource { Included = "Yes", Excluded = "No" };
        var dest = mapper.Map<IgnoreReverseDest>(source);

        Assert.Equal("Yes", dest.Included);
        Assert.Null(dest.Excluded);

        // Reverse mapping maps Excluded normally
        var reverseDest = new IgnoreReverseDest { Included = "Back", Excluded = "Also Back" };
        var reverseSource = mapper.Map<IgnoreReverseSource>(reverseDest);

        Assert.Equal("Back", reverseSource.Included);
        Assert.Equal("Also Back", reverseSource.Excluded); // Not ignored in reverse
    }

    #endregion

    #region Ignore Value Types Tests

    [Fact]
    public void Should_ignore_value_type_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ValueTypeIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new ValueTypeIgnoreSource { Name = "Test", Count = 42, Amount = 100.5m };
        var dest = mapper.Map<ValueTypeIgnoreDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(0, dest.Count); // Ignored, default value
        Assert.Equal(0m, dest.Amount); // Ignored, default value
    }

    [Fact]
    public void Should_preserve_ignored_value_type_on_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ValueTypeIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new ValueTypeIgnoreSource { Name = "New", Count = 99, Amount = 999.99m };
        var dest = new ValueTypeIgnoreDest { Name = "Old", Count = 10, Amount = 50.5m };

        mapper.Map(source, dest);

        Assert.Equal("New", dest.Name);
        Assert.Equal(10, dest.Count); // Preserved
        Assert.Equal(50.5m, dest.Amount); // Preserved
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Ignore
public class IgnoreBasicSource
{
    public string Included { get; set; } = string.Empty;
    public string Excluded { get; set; } = string.Empty;
}

public class IgnoreBasicDest
{
    public string Included { get; set; } = string.Empty;
    public string? Excluded { get; set; }
}

public class BasicIgnoreProfile : Profile
{
    public BasicIgnoreProfile()
    {
        CreateMap<IgnoreBasicSource, IgnoreBasicDest>()
            .ForMember(d => d.Excluded, opt => opt.Ignore());
    }
}

// Multi Ignore
public class MultiIgnoreSource
{
    public string Keep { get; set; } = string.Empty;
    public string Skip1 { get; set; } = string.Empty;
    public string Skip2 { get; set; } = string.Empty;
}

public class MultiIgnoreDest
{
    public string Keep { get; set; } = string.Empty;
    public string? Skip1 { get; set; }
    public string? Skip2 { get; set; }
}

public class MultiIgnoreProfile : Profile
{
    public MultiIgnoreProfile()
    {
        CreateMap<MultiIgnoreSource, MultiIgnoreDest>()
            .ForMember(d => d.Skip1, opt => opt.Ignore())
            .ForMember(d => d.Skip2, opt => opt.Ignore());
    }
}

// Nested Ignore
public class NestedIgnoreInnerSource
{
    public int Value { get; set; }
}

public class NestedIgnoreInnerDest
{
    public int Value { get; set; }
}

public class NestedIgnoreSource
{
    public string Name { get; set; } = string.Empty;
    public NestedIgnoreInnerSource? Nested { get; set; }
}

public class NestedIgnoreDest
{
    public string Name { get; set; } = string.Empty;
    public NestedIgnoreInnerDest? Nested { get; set; }
}

public class NestedIgnoreProfile : Profile
{
    public NestedIgnoreProfile()
    {
        CreateMap<NestedIgnoreInnerSource, NestedIgnoreInnerDest>();
        CreateMap<NestedIgnoreSource, NestedIgnoreDest>()
            .ForMember(d => d.Nested, opt => opt.Ignore());
    }
}

// Collection Ignore
public class CollectionIgnoreSource
{
    public string Name { get; set; } = string.Empty;
    public List<int> Items { get; set; } = new();
}

public class CollectionIgnoreDest
{
    public string Name { get; set; } = string.Empty;
    public List<int>? Items { get; set; }
}

public class CollectionIgnoreProfile : Profile
{
    public CollectionIgnoreProfile()
    {
        CreateMap<CollectionIgnoreSource, CollectionIgnoreDest>()
            .ForMember(d => d.Items, opt => opt.Ignore());
    }
}

// Combined Ignore
public class CombinedIgnoreSource
{
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class CombinedIgnoreDest
{
    public string FullName { get; set; } = string.Empty;
    public string? Secret { get; set; }
}

public class CombinedIgnoreProfile : Profile
{
    public CombinedIgnoreProfile()
    {
        CreateMap<CombinedIgnoreSource, CombinedIgnoreDest>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.First + " " + s.Last))
            .ForMember(d => d.Secret, opt => opt.Ignore());
    }
}

// Selective Ignore
public class SelectiveIgnoreSource
{
    public string A { get; set; } = string.Empty;
    public string B { get; set; } = string.Empty;
    public string C { get; set; } = string.Empty;
    public string D { get; set; } = string.Empty;
}

public class SelectiveIgnoreDest
{
    public string A { get; set; } = string.Empty;
    public string? B { get; set; }
    public string? C { get; set; }
    public string? D { get; set; }
}

public class SelectiveIgnoreProfile : Profile
{
    public SelectiveIgnoreProfile()
    {
        CreateMap<SelectiveIgnoreSource, SelectiveIgnoreDest>()
            .ForMember(d => d.B, opt => opt.Ignore())
            .ForMember(d => d.C, opt => opt.Ignore())
            .ForMember(d => d.D, opt => opt.Ignore());
    }
}

// Ignore with ReverseMap
public class IgnoreReverseSource
{
    public string Included { get; set; } = string.Empty;
    public string Excluded { get; set; } = string.Empty;
}

public class IgnoreReverseDest
{
    public string Included { get; set; } = string.Empty;
    public string? Excluded { get; set; }
}

public class IgnoreReverseProfile : Profile
{
    public IgnoreReverseProfile()
    {
        CreateMap<IgnoreReverseSource, IgnoreReverseDest>()
            .ForMember(d => d.Excluded, opt => opt.Ignore())
            .ReverseMap();
    }
}

// Value Type Ignore
public class ValueTypeIgnoreSource
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class ValueTypeIgnoreDest
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class ValueTypeIgnoreProfile : Profile
{
    public ValueTypeIgnoreProfile()
    {
        CreateMap<ValueTypeIgnoreSource, ValueTypeIgnoreDest>()
            .ForMember(d => d.Count, opt => opt.Ignore())
            .ForMember(d => d.Amount, opt => opt.Ignore());
    }
}

#endregion
