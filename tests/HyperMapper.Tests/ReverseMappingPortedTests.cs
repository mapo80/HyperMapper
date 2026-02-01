using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 ReverseMapping.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class ReverseMappingPortedTests
{
    #region Simple Reverse Mapping Tests

    [Fact]
    public void When_reverse_mapping_classes_with_simple_properties_Should_create_a_map_with_the_reverse_items()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleReverseProfile>());
        var mapper = config.CreateMapper();

        var dest = new PortedReverseDestination { Value = 10 };
        var source = mapper.Map<PortedReverseSource>(dest);

        Assert.Equal(10, source.Value);
    }

    [Fact]
    public void ReverseMap_Should_map_forward()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleReverseProfile>());
        var mapper = config.CreateMapper();

        var source = new PortedReverseSource { Value = 20 };
        var dest = mapper.Map<PortedReverseDestination>(source);

        Assert.Equal(20, dest.Value);
    }

    #endregion

    #region Reverse Mapping With Complex Properties Tests

    [Fact]
    public void ReverseMap_With_nested_objects_Should_work_both_ways()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedReverseProfile>());
        var mapper = config.CreateMapper();

        // Forward
        var source = new ParentSource { Id = 1, Child = new ChildSourceReverse { Name = "Test" } };
        var dest = mapper.Map<ParentDestination>(source);
        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Child.Name);

        // Reverse
        var destBack = new ParentDestination { Id = 2, Child = new ChildDestinationReverse { Name = "Back" } };
        var sourceBack = mapper.Map<ParentSource>(destBack);
        Assert.Equal(2, sourceBack.Id);
        Assert.Equal("Back", sourceBack.Child.Name);
    }

    #endregion

    #region Reverse Mapping With ForMember Tests

    [Fact]
    public void ReverseMap_With_ForMember_Should_work()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReverseWithForMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDifferentName { SourceValue = 42 };
        var dest = mapper.Map<DestWithDifferentName>(source);
        Assert.Equal(42, dest.DestValue);

        var sourceBack = mapper.Map<SourceWithDifferentName>(dest);
        Assert.Equal(42, sourceBack.SourceValue);
    }

    #endregion

    #region Reverse Mapping With Collections Tests

    [Fact]
    public void ReverseMap_With_collections_Should_work()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReverseCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithItems
        {
            Items = new List<ReverseItemSource>
            {
                new() { Id = 1 },
                new() { Id = 2 }
            }
        };
        var dest = mapper.Map<DestWithItems>(source);
        Assert.Equal(2, dest.Items.Count);

        var sourceBack = mapper.Map<SourceWithItems>(dest);
        Assert.Equal(2, sourceBack.Items.Count);
    }

    #endregion

    #region Methods With Reverse Tests

    [Fact]
    public void MethodsWithReverse_ShouldMapOk()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MethodsWithReverseProfile>());
        var mapper = config.CreateMapper();

        // ReverseMap should not fail due to method (OrderItemsCount)
        var dto = new OrderDtoReverse { OrderItemsCount = 5 };
        var order = mapper.Map<OrderReverse>(dto);

        Assert.Null(order.OrderItems);
    }

    #endregion

    #region ReverseMap Chain Tests

    [Fact]
    public void ReverseMap_CanBeChainedWithForMember()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ChainedReverseProfile>());
        var mapper = config.CreateMapper();

        var source = new PortedChainedSource { A = 1, B = 2 };
        var dest = mapper.Map<PortedChainedDest>(source);
        Assert.Equal(1, dest.X);
        Assert.Equal(2, dest.Y);

        var sourceBack = mapper.Map<PortedChainedSource>(dest);
        Assert.Equal(1, sourceBack.A);
        Assert.Equal(2, sourceBack.B);
    }

    #endregion

    #region ReverseMap With Ignore Tests

    [Fact]
    public void ReverseMap_With_Ignore_Should_only_ignore_in_forward_direction()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReverseWithIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithIgnored { Value = 1, Ignored = 2 };
        var dest = mapper.Map<DestWithIgnored>(source);
        Assert.Equal(1, dest.Value);
        Assert.Equal(0, dest.Ignored); // Ignored in forward

        var sourceBack = mapper.Map<SourceWithIgnored>(new DestWithIgnored { Value = 3, Ignored = 4 });
        Assert.Equal(3, sourceBack.Value);
        // Ignored is not configured in reverse, so it maps by convention
    }

    #endregion
}

#region Test Classes and Profiles

// Simple Reverse
public class PortedReverseSource
{
    public int Value { get; set; }
}

public class PortedReverseDestination
{
    public int Value { get; set; }
}

public class SimpleReverseProfile : Profile
{
    public SimpleReverseProfile()
    {
        CreateMap<PortedReverseSource, PortedReverseDestination>()
            .ReverseMap();
    }
}

// Nested Reverse
public class ChildSourceReverse
{
    public string Name { get; set; } = string.Empty;
}

public class ChildDestinationReverse
{
    public string Name { get; set; } = string.Empty;
}

public class ParentSource
{
    public int Id { get; set; }
    public ChildSourceReverse Child { get; set; } = new();
}

public class ParentDestination
{
    public int Id { get; set; }
    public ChildDestinationReverse Child { get; set; } = new();
}

public class NestedReverseProfile : Profile
{
    public NestedReverseProfile()
    {
        CreateMap<ChildSourceReverse, ChildDestinationReverse>().ReverseMap();
        CreateMap<ParentSource, ParentDestination>().ReverseMap();
    }
}

// Different Name Reverse
public class SourceWithDifferentName
{
    public int SourceValue { get; set; }
}

public class DestWithDifferentName
{
    public int DestValue { get; set; }
}

public class ReverseWithForMemberProfile : Profile
{
    public ReverseWithForMemberProfile()
    {
        CreateMap<SourceWithDifferentName, DestWithDifferentName>()
            .ForMember(d => d.DestValue, opt => opt.MapFrom(s => s.SourceValue))
            .ReverseMap()
            .ForMember(s => s.SourceValue, opt => opt.MapFrom(d => d.DestValue));
    }
}

// Collection Reverse
public class ReverseItemSource
{
    public int Id { get; set; }
}

public class ReverseItemDest
{
    public int Id { get; set; }
}

public class SourceWithItems
{
    public List<ReverseItemSource> Items { get; set; } = new();
}

public class DestWithItems
{
    public List<ReverseItemDest> Items { get; set; } = new();
}

public class ReverseCollectionProfile : Profile
{
    public ReverseCollectionProfile()
    {
        CreateMap<ReverseItemSource, ReverseItemDest>().ReverseMap();
        CreateMap<SourceWithItems, DestWithItems>().ReverseMap();
    }
}

// Methods With Reverse
public class OrderReverse
{
    public OrderItemReverse[]? OrderItems { get; set; }
}

public class OrderItemReverse
{
    public string Product { get; set; } = string.Empty;
}

public class OrderDtoReverse
{
    public int OrderItemsCount { get; set; }
}

public class MethodsWithReverseProfile : Profile
{
    public MethodsWithReverseProfile()
    {
        CreateMap<OrderReverse, OrderDtoReverse>()
            .ReverseMap();
    }
}

// Chained Reverse
public class PortedChainedSource
{
    public int A { get; set; }
    public int B { get; set; }
}

public class PortedChainedDest
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class ChainedReverseProfile : Profile
{
    public ChainedReverseProfile()
    {
        CreateMap<PortedChainedSource, PortedChainedDest>()
            .ForMember(d => d.X, opt => opt.MapFrom(s => s.A))
            .ForMember(d => d.Y, opt => opt.MapFrom(s => s.B))
            .ReverseMap()
            .ForMember(s => s.A, opt => opt.MapFrom(d => d.X))
            .ForMember(s => s.B, opt => opt.MapFrom(d => d.Y));
    }
}

// Ignore Reverse
public class SourceWithIgnored
{
    public int Value { get; set; }
    public int Ignored { get; set; }
}

public class DestWithIgnored
{
    public int Value { get; set; }
    public int Ignored { get; set; }
}

public class ReverseWithIgnoreProfile : Profile
{
    public ReverseWithIgnoreProfile()
    {
        CreateMap<SourceWithIgnored, DestWithIgnored>()
            .ForMember(d => d.Ignored, opt => opt.Ignore())
            .ReverseMap();
    }
}

#endregion
