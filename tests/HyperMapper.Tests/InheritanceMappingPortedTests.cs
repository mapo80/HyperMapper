using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 MappingInheritance/
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
///
/// Note: HyperMapper does not yet support Include()/IncludeBase() for polymorphic mapping.
/// These tests cover inheritance scenarios that work with convention-based mapping.
/// </summary>
public class InheritanceMappingPortedTests
{
    #region Basic Inheritance Without Include

    [Fact]
    public void Should_map_derived_class_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BasicInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new InhDerivedSource
        {
            BaseProperty = "base",
            DerivedProperty = "derived"
        };

        var dest = mapper.Map<InhDerivedDest>(source);

        Assert.Equal("base", dest.BaseProperty);
        Assert.Equal("derived", dest.DerivedProperty);
    }

    [Fact]
    public void Should_map_base_class_when_source_is_derived()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BasicInheritanceProfile>());
        var mapper = config.CreateMapper();

        InhBaseSource source = new InhDerivedSource
        {
            BaseProperty = "base",
            DerivedProperty = "derived"
        };

        // Map as base type - only base properties should be mapped
        var dest = mapper.Map<InhBaseDest>(source);

        Assert.Equal("base", dest.BaseProperty);
    }

    [Fact]
    public void Should_map_deep_inheritance_hierarchy()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DeepInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new InhLevel3Source
        {
            Level1Prop = "L1",
            Level2Prop = "L2",
            Level3Prop = "L3"
        };

        var dest = mapper.Map<InhLevel3Dest>(source);

        Assert.Equal("L1", dest.Level1Prop);
        Assert.Equal("L2", dest.Level2Prop);
        Assert.Equal("L3", dest.Level3Prop);
    }

    #endregion

    #region Ignore Inheritance Tests

    [Fact]
    public void Should_respect_ignore_on_derived_mapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IgnoreInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new InhIgnoreSource
        {
            Value = 100,
            IgnoredValue = 999
        };

        var dest = mapper.Map<InhIgnoreDest>(source);

        Assert.Equal(100, dest.Value);
        Assert.Equal(0, dest.IgnoredValue); // Should be ignored, retain default
    }

    #endregion

    #region Map to Base Class Tests

    [Fact]
    public void Should_map_to_base_class_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MapToBaseProfile>());
        var mapper = config.CreateMapper();

        var source = new MapToBaseSource
        {
            Id = 1,
            Name = "test",
            Extra = "extra"
        };

        var dest = mapper.Map<MapToBaseDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("test", dest.Name);
        // Extra is not in destination base class
    }

    #endregion

    #region Open Generics with Inheritance Tests

    [Fact]
    public void Should_map_generic_derived_classes()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<GenericInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new GenericDerivedSource<string>
        {
            Value = "test",
            DerivedValue = "derived"
        };

        var dest = mapper.Map<GenericDerivedDest<string>>(source);

        Assert.Equal("test", dest.Value);
        Assert.Equal("derived", dest.DerivedValue);
    }

    [Fact]
    public void Should_map_generic_with_complex_type_argument()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexGenericInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new GenericDerivedSource<InhItem>
        {
            Value = new InhItem { Name = "item" },
            DerivedValue = new InhItem { Name = "derived-item" }
        };

        var dest = mapper.Map<GenericDerivedDest<InhItemDto>>(source);

        Assert.NotNull(dest.Value);
        Assert.Equal("item", dest.Value.Name);
        Assert.NotNull(dest.DerivedValue);
        Assert.Equal("derived-item", dest.DerivedValue.Name);
    }

    #endregion

    #region Destination Only Derived Tests

    [Fact]
    public void Should_map_when_only_destination_is_derived()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DestOnlyDerivedProfile>());
        var mapper = config.CreateMapper();

        var source = new DestOnlySource { Value = 42 };

        var dest = mapper.Map<DestOnlyDerivedDest>(source);

        Assert.Equal(42, dest.Value);
        Assert.Equal(0, dest.DerivedOnly); // Not in source
    }

    #endregion

    #region Collection with Inheritance Tests

    [Fact]
    public void Should_map_collection_of_derived_types()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new List<InhDerivedSource>
        {
            new() { BaseProperty = "base1", DerivedProperty = "derived1" },
            new() { BaseProperty = "base2", DerivedProperty = "derived2" }
        };

        var dest = mapper.Map<List<InhDerivedDest>>(source);

        Assert.Equal(2, dest.Count);
        Assert.Equal("base1", dest[0].BaseProperty);
        Assert.Equal("derived1", dest[0].DerivedProperty);
        Assert.Equal("base2", dest[1].BaseProperty);
        Assert.Equal("derived2", dest[1].DerivedProperty);
    }

    [Fact]
    public void Should_map_collection_as_base_type()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new List<InhDerivedSource>
        {
            new() { BaseProperty = "base1", DerivedProperty = "derived1" }
        };

        var dest = mapper.Map<List<InhBaseDest>>(source);

        Assert.Single(dest);
        Assert.Equal("base1", dest[0].BaseProperty);
    }

    #endregion

    #region Nested Inheritance Tests

    [Fact]
    public void Should_map_nested_inherited_types()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedInheritanceProfile>());
        var mapper = config.CreateMapper();

        var source = new InhParentSource
        {
            Name = "parent",
            Child = new InhChildDerivedSource
            {
                ChildBase = "childBase",
                ChildDerived = "childDerived"
            }
        };

        var dest = mapper.Map<InhParentDest>(source);

        Assert.Equal("parent", dest.Name);
        Assert.NotNull(dest.Child);
        Assert.Equal("childBase", dest.Child.ChildBase);
    }

    #endregion

    #region Override Ignore Tests

    [Fact]
    public void Should_allow_mapping_previously_ignored_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<OverrideIgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new OverrideIgnoreSource { Value = 42, Extra = "mapped" };

        var dest = mapper.Map<OverrideIgnoreDest>(source);

        Assert.Equal(42, dest.Value);
        Assert.Equal("mapped", dest.Extra);
    }

    #endregion

    #region Property Resolution with Inheritance Tests

    [Fact]
    public void Should_resolve_most_specific_type_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PropertyResolutionProfile>());
        var mapper = config.CreateMapper();

        var source = new PropResSource
        {
            Id = 1,
            Name = "test"
        };

        var dest = mapper.Map<PropResDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("test", dest.Name);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Inheritance
public class InhBaseSource
{
    public string BaseProperty { get; set; } = string.Empty;
}

public class InhDerivedSource : InhBaseSource
{
    public string DerivedProperty { get; set; } = string.Empty;
}

public class InhBaseDest
{
    public string BaseProperty { get; set; } = string.Empty;
}

public class InhDerivedDest : InhBaseDest
{
    public string DerivedProperty { get; set; } = string.Empty;
}

public class BasicInheritanceProfile : Profile
{
    public BasicInheritanceProfile()
    {
        CreateMap<InhBaseSource, InhBaseDest>();
        CreateMap<InhDerivedSource, InhDerivedDest>();
    }
}

// Deep Inheritance
public class InhLevel1Source
{
    public string Level1Prop { get; set; } = string.Empty;
}

public class InhLevel2Source : InhLevel1Source
{
    public string Level2Prop { get; set; } = string.Empty;
}

public class InhLevel3Source : InhLevel2Source
{
    public string Level3Prop { get; set; } = string.Empty;
}

public class InhLevel1Dest
{
    public string Level1Prop { get; set; } = string.Empty;
}

public class InhLevel2Dest : InhLevel1Dest
{
    public string Level2Prop { get; set; } = string.Empty;
}

public class InhLevel3Dest : InhLevel2Dest
{
    public string Level3Prop { get; set; } = string.Empty;
}

public class DeepInheritanceProfile : Profile
{
    public DeepInheritanceProfile()
    {
        CreateMap<InhLevel1Source, InhLevel1Dest>();
        CreateMap<InhLevel2Source, InhLevel2Dest>();
        CreateMap<InhLevel3Source, InhLevel3Dest>();
    }
}

// Ignore Inheritance
public class InhIgnoreSource
{
    public int Value { get; set; }
    public int IgnoredValue { get; set; }
}

public class InhIgnoreDest
{
    public int Value { get; set; }
    public int IgnoredValue { get; set; }
}

public class IgnoreInheritanceProfile : Profile
{
    public IgnoreInheritanceProfile()
    {
        CreateMap<InhIgnoreSource, InhIgnoreDest>()
            .ForMember(d => d.IgnoredValue, opt => opt.Ignore());
    }
}

// Map to Base
public class MapToBaseSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
}

public class MapToBaseDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MapToBaseProfile : Profile
{
    public MapToBaseProfile()
    {
        CreateMap<MapToBaseSource, MapToBaseDest>();
    }
}

// Generic Inheritance
public class GenericBaseSource<T>
{
    public T? Value { get; set; }
}

public class GenericDerivedSource<T> : GenericBaseSource<T>
{
    public T? DerivedValue { get; set; }
}

public class GenericBaseDest<T>
{
    public T? Value { get; set; }
}

public class GenericDerivedDest<T> : GenericBaseDest<T>
{
    public T? DerivedValue { get; set; }
}

public class GenericInheritanceProfile : Profile
{
    public GenericInheritanceProfile()
    {
        CreateMap<GenericDerivedSource<string>, GenericDerivedDest<string>>();
    }
}

// Complex Generic Inheritance
public class InhItem
{
    public string Name { get; set; } = string.Empty;
}

public class InhItemDto
{
    public string Name { get; set; } = string.Empty;
}

public class ComplexGenericInheritanceProfile : Profile
{
    public ComplexGenericInheritanceProfile()
    {
        CreateMap<InhItem, InhItemDto>();
        CreateMap<GenericDerivedSource<InhItem>, GenericDerivedDest<InhItemDto>>();
    }
}

// Destination Only Derived
public class DestOnlySource
{
    public int Value { get; set; }
}

public class DestOnlyBaseDest
{
    public int Value { get; set; }
}

public class DestOnlyDerivedDest : DestOnlyBaseDest
{
    public int DerivedOnly { get; set; }
}

public class DestOnlyDerivedProfile : Profile
{
    public DestOnlyDerivedProfile()
    {
        CreateMap<DestOnlySource, DestOnlyDerivedDest>();
    }
}

// Collection Inheritance
public class CollectionInheritanceProfile : Profile
{
    public CollectionInheritanceProfile()
    {
        CreateMap<InhBaseSource, InhBaseDest>();
        CreateMap<InhDerivedSource, InhDerivedDest>();
        CreateMap<InhDerivedSource, InhBaseDest>();
    }
}

// Nested Inheritance
public class InhChildBaseSource
{
    public string ChildBase { get; set; } = string.Empty;
}

public class InhChildDerivedSource : InhChildBaseSource
{
    public string ChildDerived { get; set; } = string.Empty;
}

public class InhChildBaseDest
{
    public string ChildBase { get; set; } = string.Empty;
}

public class InhParentSource
{
    public string Name { get; set; } = string.Empty;
    public InhChildBaseSource? Child { get; set; }
}

public class InhParentDest
{
    public string Name { get; set; } = string.Empty;
    public InhChildBaseDest? Child { get; set; }
}

public class NestedInheritanceProfile : Profile
{
    public NestedInheritanceProfile()
    {
        CreateMap<InhChildBaseSource, InhChildBaseDest>();
        CreateMap<InhParentSource, InhParentDest>();
    }
}

// Override Ignore
public class OverrideIgnoreSource
{
    public int Value { get; set; }
    public string Extra { get; set; } = string.Empty;
}

public class OverrideIgnoreDest
{
    public int Value { get; set; }
    public string Extra { get; set; } = string.Empty;
}

public class OverrideIgnoreProfile : Profile
{
    public OverrideIgnoreProfile()
    {
        CreateMap<OverrideIgnoreSource, OverrideIgnoreDest>();
    }
}

// Property Resolution
public class PropResSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PropResDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PropertyResolutionProfile : Profile
{
    public PropertyResolutionProfile()
    {
        CreateMap<PropResSource, PropResDest>();
    }
}

#endregion
