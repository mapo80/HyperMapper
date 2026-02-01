using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for scoped mapping scenarios ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
///
/// Note: HyperMapper creates mapper instances from MapperConfiguration.
/// These tests verify mapper instance behavior and configuration isolation.
/// </summary>
public class ScopedMappingPortedTests
{
    #region Mapper Instance Isolation Tests

    [Fact]
    public void Different_configurations_should_be_isolated()
    {
        var config1 = new MapperConfiguration(cfg =>
            cfg.AddProfile<ScopedProfile1>());
        var config2 = new MapperConfiguration(cfg =>
            cfg.AddProfile<ScopedProfile2>());

        var mapper1 = config1.CreateMapper();
        var mapper2 = config2.CreateMapper();

        var source = new ScopedSource { Value = 10 };

        var dest1 = mapper1.Map<ScopedDest1>(source);
        var dest2 = mapper2.Map<ScopedDest2>(source);

        Assert.Equal(10, dest1.Value);
        Assert.Equal(10, dest2.Value);
    }

    [Fact]
    public void Mapper_instances_from_same_config_should_behave_identically()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ScopedProfile1>());

        var mapper1 = config.CreateMapper();
        var mapper2 = config.CreateMapper();

        var source = new ScopedSource { Value = 42 };

        var dest1 = mapper1.Map<ScopedDest1>(source);
        var dest2 = mapper2.Map<ScopedDest1>(source);

        Assert.Equal(dest1.Value, dest2.Value);
    }

    #endregion

    #region Multiple Profile Tests

    [Fact]
    public void Configuration_with_multiple_profiles_should_include_all_mappings()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ScopedProfile1>();
            cfg.AddProfile<ScopedProfile2>();
        });
        var mapper = config.CreateMapper();

        var source = new ScopedSource { Value = 100 };

        var dest1 = mapper.Map<ScopedDest1>(source);
        var dest2 = mapper.Map<ScopedDest2>(source);

        Assert.Equal(100, dest1.Value);
        Assert.Equal(100, dest2.Value);
    }

    [Fact]
    public void Profile_instances_should_work()
    {
        var profile1 = new ScopedProfile1();
        var profile2 = new ScopedProfile2();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(profile1);
            cfg.AddProfile(profile2);
        });
        var mapper = config.CreateMapper();

        var source = new ScopedSource { Value = 50 };

        var dest1 = mapper.Map<ScopedDest1>(source);
        Assert.Equal(50, dest1.Value);
    }

    #endregion

    #region Nested Mapping Scope Tests

    [Fact]
    public void Nested_mappings_should_use_same_mapper_instance()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedScopedProfile>());
        var mapper = config.CreateMapper();

        var source = new ScopedParent
        {
            Name = "Parent",
            Child = new ScopedChild { ChildValue = 123 }
        };

        var dest = mapper.Map<ScopedParentDest>(source);

        Assert.Equal("Parent", dest.Name);
        Assert.NotNull(dest.Child);
        Assert.Equal(123, dest.Child.ChildValue);
    }

    [Fact]
    public void Collection_mapping_should_use_same_mapper_instance()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionScopedProfile>());
        var mapper = config.CreateMapper();

        var source = new ScopedParentWithList
        {
            Name = "Parent",
            Children = new List<ScopedChild>
            {
                new() { ChildValue = 1 },
                new() { ChildValue = 2 }
            }
        };

        var dest = mapper.Map<ScopedParentWithListDest>(source);

        Assert.Equal("Parent", dest.Name);
        Assert.Equal(2, dest.Children.Count);
        Assert.Equal(1, dest.Children[0].ChildValue);
        Assert.Equal(2, dest.Children[1].ChildValue);
    }

    #endregion

    #region Converter with Context Tests

    [Fact]
    public void Converter_should_have_access_to_mapper_via_context()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ConverterScopedProfile>());
        var mapper = config.CreateMapper();

        var source = new WrapperScopedSource
        {
            Inner = new ScopedSource { Value = 999 }
        };

        var dest = mapper.Map<WrapperScopedDest>(source);

        Assert.NotNull(dest.Inner);
        Assert.Equal(999, dest.Inner.Value);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Scoped Classes
public class ScopedSource
{
    public int Value { get; set; }
}

public class ScopedDest1
{
    public int Value { get; set; }
}

public class ScopedDest2
{
    public int Value { get; set; }
}

public class ScopedProfile1 : Profile
{
    public ScopedProfile1()
    {
        CreateMap<ScopedSource, ScopedDest1>();
    }
}

public class ScopedProfile2 : Profile
{
    public ScopedProfile2()
    {
        CreateMap<ScopedSource, ScopedDest2>();
    }
}

// Nested Scoped Classes
public class ScopedChild
{
    public int ChildValue { get; set; }
}

public class ScopedChildDest
{
    public int ChildValue { get; set; }
}

public class ScopedParent
{
    public string Name { get; set; } = string.Empty;
    public ScopedChild? Child { get; set; }
}

public class ScopedParentDest
{
    public string Name { get; set; } = string.Empty;
    public ScopedChildDest? Child { get; set; }
}

public class NestedScopedProfile : Profile
{
    public NestedScopedProfile()
    {
        CreateMap<ScopedChild, ScopedChildDest>();
        CreateMap<ScopedParent, ScopedParentDest>();
    }
}

// Collection Scoped Classes
public class ScopedParentWithList
{
    public string Name { get; set; } = string.Empty;
    public List<ScopedChild> Children { get; set; } = new();
}

public class ScopedParentWithListDest
{
    public string Name { get; set; } = string.Empty;
    public List<ScopedChildDest> Children { get; set; } = new();
}

public class CollectionScopedProfile : Profile
{
    public CollectionScopedProfile()
    {
        CreateMap<ScopedChild, ScopedChildDest>();
        CreateMap<ScopedParentWithList, ScopedParentWithListDest>();
    }
}

// Converter Scoped Classes
public class WrapperScopedSource
{
    public ScopedSource? Inner { get; set; }
}

public class WrapperScopedDest
{
    public ScopedDest1? Inner { get; set; }
}

public class WrapperScopedConverter : ITypeConverter<WrapperScopedSource, WrapperScopedDest>
{
    public WrapperScopedDest Convert(WrapperScopedSource source, WrapperScopedDest destination, ResolutionContext context)
    {
        return new WrapperScopedDest
        {
            Inner = source.Inner != null ? context.Mapper.Map<ScopedDest1>(source.Inner) : null
        };
    }
}

public class ConverterScopedProfile : Profile
{
    public ConverterScopedProfile()
    {
        CreateMap<ScopedSource, ScopedDest1>();
        CreateMap<WrapperScopedSource, WrapperScopedDest>()
            .ConvertUsing<WrapperScopedConverter>();
    }
}

#endregion
