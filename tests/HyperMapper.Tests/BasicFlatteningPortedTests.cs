using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 BasicFlattening.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class BasicFlatteningPortedTests
{
    #region Basic Property Flattening Tests

    [Fact]
    public void Should_map_flattened_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<FlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new ModelObject
        {
            BaseDate = new DateTime(2024, 6, 15),
            Sub = new ModelSubObject
            {
                ProperName = "Some proper name"
            },
            Sub2 = new ModelSubObject
            {
                ProperName = "Some other proper name"
            },
            SubWithExtraName = new ModelSubObject
            {
                ProperName = "Yet another proper name"
            }
        };

        var dest = mapper.Map<ModelDto>(source);

        Assert.Equal("Some proper name", dest.SubProperName);
        Assert.Equal("Some other proper name", dest.Sub2ProperName);
        Assert.Equal("Yet another proper name", dest.SubWithExtraNameProperName);
    }

    #endregion

    #region Nested Property Flattening Tests

    [Fact]
    public void Should_map_nested_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new OuterSource
        {
            Inner = new InnerSource
            {
                Value = 42
            }
        };

        var dest = mapper.Map<OuterDest>(source);

        Assert.Equal(42, dest.InnerValue);
    }

    [Fact]
    public void Should_map_deeply_nested_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DeepFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new FlatteningLevel1
        {
            Level2 = new FlatteningLevel2Source
            {
                Level3 = new FlatteningLevel3Source
                {
                    Name = "Deep value"
                }
            }
        };

        var dest = mapper.Map<FlatteningFlatDest>(source);

        Assert.Equal("Deep value", dest.Level2Level3Name);
    }

    #endregion

    #region Null Handling in Flattening Tests

    [Fact]
    public void Should_handle_null_inner_object()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new OuterSource { Inner = null };

        var dest = mapper.Map<OuterDest>(source);

        Assert.Equal(0, dest.InnerValue); // Default value
    }

    [Fact]
    public void Should_handle_null_in_chain()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DeepFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new FlatteningLevel1 { Level2 = null };

        var dest = mapper.Map<FlatteningFlatDest>(source);

        Assert.Null(dest.Level2Level3Name); // Null because chain is broken
    }

    [Fact]
    public void Should_handle_partial_null_chain()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DeepFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new FlatteningLevel1
        {
            Level2 = new FlatteningLevel2Source { Level3 = null }
        };

        var dest = mapper.Map<FlatteningFlatDest>(source);

        Assert.Null(dest.Level2Level3Name);
    }

    #endregion

    #region Flattening With Collections Tests

    [Fact]
    public void Should_map_with_nested_collection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new FlatteningParentWithChildren
        {
            Id = 1,
            Children = new List<FlatteningChildItem>
            {
                new() { Name = "Child1" },
                new() { Name = "Child2" }
            }
        };

        var dest = mapper.Map<FlatteningParentDto>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(2, dest.Children.Count);
        Assert.Equal("Child1", dest.Children[0].Name);
        Assert.Equal("Child2", dest.Children[1].Name);
    }

    #endregion

    #region Self-Reference Flattening Tests

    [Fact]
    public void Should_map_self_referencing_type()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<SelfRefFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new TreeNode
        {
            Value = "Root",
            Children = new List<TreeNode>
            {
                new() { Value = "Child1" },
                new() { Value = "Child2" }
            }
        };

        var dest = mapper.Map<TreeNodeDto>(source);

        Assert.Equal("Root", dest.Value);
        Assert.Equal(2, dest.Children!.Count);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Flattening
public class ModelObject
{
    public DateTime BaseDate { get; set; }
    public ModelSubObject? Sub { get; set; }
    public ModelSubObject? Sub2 { get; set; }
    public ModelSubObject? SubWithExtraName { get; set; }
}

public class ModelSubObject
{
    public string ProperName { get; set; } = string.Empty;
}

public class ModelDto
{
    public DateTime BaseDate { get; set; }
    public string? SubProperName { get; set; }
    public string? Sub2ProperName { get; set; }
    public string? SubWithExtraNameProperName { get; set; }
}

public class FlatteningProfile : Profile
{
    public FlatteningProfile()
    {
        CreateMap<ModelObject, ModelDto>()
            .ForMember(d => d.SubProperName, opt => opt.MapFrom(s => s.Sub != null ? s.Sub.ProperName : null))
            .ForMember(d => d.Sub2ProperName, opt => opt.MapFrom(s => s.Sub2 != null ? s.Sub2.ProperName : null))
            .ForMember(d => d.SubWithExtraNameProperName, opt => opt.MapFrom(s => s.SubWithExtraName != null ? s.SubWithExtraName.ProperName : null));
    }
}

// Nested Flattening
public class OuterSource
{
    public InnerSource? Inner { get; set; }
}

public class InnerSource
{
    public int Value { get; set; }
}

public class OuterDest
{
    public int InnerValue { get; set; }
}

public class NestedFlatteningProfile : Profile
{
    public NestedFlatteningProfile()
    {
        CreateMap<OuterSource, OuterDest>()
            .ForMember(d => d.InnerValue, opt => opt.MapFrom(s => s.Inner != null ? s.Inner.Value : 0));
    }
}

// Deep Flattening
public class FlatteningLevel1
{
    public FlatteningLevel2Source? Level2 { get; set; }
}

public class FlatteningLevel2Source
{
    public FlatteningLevel3Source? Level3 { get; set; }
}

public class FlatteningLevel3Source
{
    public string Name { get; set; } = string.Empty;
}

public class FlatteningFlatDest
{
    public string? Level2Level3Name { get; set; }
}

public class DeepFlatteningProfile : Profile
{
    public DeepFlatteningProfile()
    {
        CreateMap<FlatteningLevel1, FlatteningFlatDest>()
            .ForMember(d => d.Level2Level3Name, opt => opt.MapFrom(s => s.Level2 != null && s.Level2.Level3 != null ? s.Level2.Level3.Name : null));
    }
}

// Collection Flattening
public class FlatteningParentWithChildren
{
    public int Id { get; set; }
    public List<FlatteningChildItem> Children { get; set; } = new();
}

public class FlatteningChildItem
{
    public string Name { get; set; } = string.Empty;
}

public class FlatteningParentDto
{
    public int Id { get; set; }
    public List<FlatteningChildItemDto> Children { get; set; } = new();
}

public class FlatteningChildItemDto
{
    public string Name { get; set; } = string.Empty;
}

public class CollectionFlatteningProfile : Profile
{
    public CollectionFlatteningProfile()
    {
        CreateMap<FlatteningChildItem, FlatteningChildItemDto>();
        CreateMap<FlatteningParentWithChildren, FlatteningParentDto>();
    }
}

// Self-Reference Flattening
public class TreeNode
{
    public string Value { get; set; } = string.Empty;
    public List<TreeNode>? Children { get; set; }
}

public class TreeNodeDto
{
    public string Value { get; set; } = string.Empty;
    public List<TreeNodeDto>? Children { get; set; }
}

public class SelfRefFlatteningProfile : Profile
{
    public SelfRefFlatteningProfile()
    {
        CreateMap<TreeNode, TreeNodeDto>();
    }
}

#endregion
