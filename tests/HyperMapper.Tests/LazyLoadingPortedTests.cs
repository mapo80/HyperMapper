using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for Lazy&lt;T&gt; mapping ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
///
/// Note: HyperMapper handles Lazy&lt;T&gt; by evaluating the value during mapping.
/// </summary>
public class LazyLoadingPortedTests
{
    #region Basic Lazy Property Tests

    [Fact]
    public void Should_map_lazy_value_to_regular_value()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<LazyToValueProfile>());
        var mapper = config.CreateMapper();

        var source = new LazySource
        {
            Name = "Test",
            LazyValue = new Lazy<int>(() => 42)
        };

        var dest = mapper.Map<NonLazyDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void Should_map_regular_value_to_lazy()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ValueToLazyProfile>());
        var mapper = config.CreateMapper();

        var source = new NonLazySource
        {
            Name = "Test",
            Value = 100
        };

        var dest = mapper.Map<LazyDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.NotNull(dest.LazyValue);
        Assert.Equal(100, dest.LazyValue.Value);
    }

    #endregion

    #region Lazy with Complex Types Tests

    [Fact]
    public void Should_map_lazy_complex_object()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<LazyComplexProfile>());
        var mapper = config.CreateMapper();

        var innerObject = new LazyInnerSource { Id = 1, Data = "Inner Data" };
        var source = new LazyComplexSource
        {
            Name = "Outer",
            LazyInner = new Lazy<LazyInnerSource>(() => innerObject)
        };

        var dest = mapper.Map<LazyComplexDest>(source);

        Assert.Equal("Outer", dest.Name);
        Assert.NotNull(dest.Inner);
        Assert.Equal(1, dest.Inner.Id);
        Assert.Equal("Inner Data", dest.Inner.Data);
    }

    #endregion

    #region Lazy Collection Tests

    [Fact]
    public void Should_map_lazy_collection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<LazyCollectionProfile>());
        var mapper = config.CreateMapper();

        var items = new List<LazyItemSource>
        {
            new() { ItemId = 1 },
            new() { ItemId = 2 }
        };

        var source = new LazyCollectionSource
        {
            LazyItems = new Lazy<List<LazyItemSource>>(() => items)
        };

        var dest = mapper.Map<LazyCollectionDest>(source);

        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal(1, dest.Items[0].ItemId);
    }

    #endregion

    #region Nullable Lazy Tests

    [Fact]
    public void Should_handle_null_lazy()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<LazyToValueProfile>());
        var mapper = config.CreateMapper();

        var source = new LazySource
        {
            Name = "Test",
            LazyValue = null!
        };

        var dest = mapper.Map<NonLazyDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(0, dest.Value); // Default value
    }

    #endregion
}

#region Test Classes and Profiles

// Lazy to Value
public class LazySource
{
    public string Name { get; set; } = string.Empty;
    public Lazy<int>? LazyValue { get; set; }
}

public class NonLazyDest
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class LazyToValueProfile : Profile
{
    public LazyToValueProfile()
    {
        CreateMap<LazySource, NonLazyDest>()
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.LazyValue != null ? s.LazyValue.Value : 0));
    }
}

// Value to Lazy
public class NonLazySource
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class LazyDest
{
    public string Name { get; set; } = string.Empty;
    public Lazy<int>? LazyValue { get; set; }
}

public class ValueToLazyProfile : Profile
{
    public ValueToLazyProfile()
    {
        CreateMap<NonLazySource, LazyDest>()
            .ForMember(d => d.LazyValue, opt => opt.MapFrom(s => new Lazy<int>(() => s.Value)));
    }
}

// Lazy Complex
public class LazyInnerSource
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class LazyInnerDest
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class LazyComplexSource
{
    public string Name { get; set; } = string.Empty;
    public Lazy<LazyInnerSource>? LazyInner { get; set; }
}

public class LazyComplexDest
{
    public string Name { get; set; } = string.Empty;
    public LazyInnerDest? Inner { get; set; }
}

public class LazyComplexProfile : Profile
{
    public LazyComplexProfile()
    {
        CreateMap<LazyInnerSource, LazyInnerDest>();
        CreateMap<LazyComplexSource, LazyComplexDest>()
            .ForMember(d => d.Inner, opt => opt.MapFrom(s => s.LazyInner != null ? s.LazyInner.Value : null));
    }
}

// Lazy Collection
public class LazyItemSource
{
    public int ItemId { get; set; }
}

public class LazyItemDest
{
    public int ItemId { get; set; }
}

public class LazyCollectionSource
{
    public Lazy<List<LazyItemSource>>? LazyItems { get; set; }
}

public class LazyCollectionDest
{
    public List<LazyItemDest> Items { get; set; } = new();
}

public class LazyCollectionProfile : Profile
{
    public LazyCollectionProfile()
    {
        CreateMap<LazyItemSource, LazyItemDest>();
        CreateMap<LazyCollectionSource, LazyCollectionDest>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.LazyItems != null ? s.LazyItems.Value : new List<LazyItemSource>()));
    }
}

#endregion
