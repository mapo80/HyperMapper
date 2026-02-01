using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 NullBehavior.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class NullBehaviorPortedTests
{
    #region Null Source Tests

    [Fact]
    public void When_mapping_a_null_model_Should_return_null()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullModelProfile>());
        var mapper = config.CreateMapper();

        NullModelSource? source = null;
        var dest = mapper.Map<NullModelDest>(source!);

        Assert.Null(dest);
    }

    [Fact]
    public void Map_WithExplicitTypes_NullSource_ReturnsNull()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullModelProfile>());
        var mapper = config.CreateMapper();

        NullModelSource? source = null;
        var dest = mapper.Map<NullModelSource, NullModelDest>(source!);

        Assert.Null(dest);
    }

    #endregion

    #region Null To Existing Destination Tests

    [Fact]
    public void NullToExistingDestination_Should_return_the_destination()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullModelProfile>());
        var mapper = config.CreateMapper();

        NullModelSource? source = null;
        var existingDest = new NullModelDest { Value = 42 };

        var result = mapper.Map(source!, existingDest);

        Assert.Same(existingDest, result);
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region Null Property Mapping Tests

    [Fact]
    public void When_mapping_a_model_with_null_nested_object_Should_map_to_null()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new NullNestedSource { Id = 1, Nested = null };
        var dest = mapper.Map<NullNestedDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Null(dest.Nested);
    }

    [Fact]
    public void When_mapping_a_model_with_null_string_Should_map_to_null()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullStringProfile>());
        var mapper = config.CreateMapper();

        var source = new NullStringSource { Name = null };
        var dest = mapper.Map<NullStringDest>(source);

        Assert.Null(dest.Name);
    }

    #endregion

    #region Null Collection Mapping Tests

    [Fact]
    public void When_mapping_null_collection_Should_return_empty_collection()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new NullCollectionSource { Items = null };
        var dest = mapper.Map<NullCollectionDest>(source);

        Assert.NotNull(dest.Items);
        Assert.Empty(dest.Items);
    }

    [Fact]
    public void When_mapping_null_array_Should_return_empty_array()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullArrayProfile>());
        var mapper = config.CreateMapper();

        var source = new NullArraySource { Values = null };
        var dest = mapper.Map<NullArrayDest>(source);

        Assert.NotNull(dest.Values);
        Assert.Empty(dest.Values);
    }

    #endregion

    #region Null In MapFrom Chain Tests

    [Fact]
    public void When_mapping_with_null_in_chain_Should_return_default()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullChainProfile>());
        var mapper = config.CreateMapper();

        var source = new ChainSource { Inner = null };
        var dest = mapper.Map<ChainDest>(source);

        Assert.Equal(0, dest.InnerValue);
    }

    [Fact]
    public void When_mapping_with_value_in_chain_Should_return_value()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullChainProfile>());
        var mapper = config.CreateMapper();

        var source = new ChainSource { Inner = new ChainInner { Value = 42 } };
        var dest = mapper.Map<ChainDest>(source);

        Assert.Equal(42, dest.InnerValue);
    }

    #endregion

    #region Nullable Type Converter Tests

    [Fact]
    public void When_specifying_a_resolver_for_a_nullable_type_Should_allow_the_resolver_to_handle_null_values()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableBoolSource { IsFooBarred = null };
        var dest = mapper.Map<NullableBoolDest>(source);

        Assert.Equal("(n/a)", dest.IsFooBarred);
    }

    [Fact]
    public void When_specifying_a_resolver_for_a_nullable_type_With_true_Should_return_Yes()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableBoolSource { IsFooBarred = true };
        var dest = mapper.Map<NullableBoolDest>(source);

        Assert.Equal("Yes", dest.IsFooBarred);
    }

    [Fact]
    public void When_specifying_a_resolver_for_a_nullable_type_With_false_Should_return_No()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableBoolSource { IsFooBarred = false };
        var dest = mapper.Map<NullableBoolDest>(source);

        Assert.Equal("No", dest.IsFooBarred);
    }

    #endregion
}

#region Test Classes and Profiles

// Null Model
public class NullModelSource
{
    public int Value { get; set; }
}

public class NullModelDest
{
    public int Value { get; set; }
}

public class NullModelProfile : Profile
{
    public NullModelProfile()
    {
        CreateMap<NullModelSource, NullModelDest>();
    }
}

// Null Nested
public class NestedSourceObject
{
    public string Name { get; set; } = string.Empty;
}

public class NestedDestObject
{
    public string Name { get; set; } = string.Empty;
}

public class NullNestedSource
{
    public int Id { get; set; }
    public NestedSourceObject? Nested { get; set; }
}

public class NullNestedDest
{
    public int Id { get; set; }
    public NestedDestObject? Nested { get; set; }
}

public class NullNestedProfile : Profile
{
    public NullNestedProfile()
    {
        CreateMap<NestedSourceObject, NestedDestObject>();
        CreateMap<NullNestedSource, NullNestedDest>();
    }
}

// Null String
public class NullStringSource
{
    public string? Name { get; set; }
}

public class NullStringDest
{
    public string? Name { get; set; }
}

public class NullStringProfile : Profile
{
    public NullStringProfile()
    {
        CreateMap<NullStringSource, NullStringDest>();
    }
}

// Null Collection
public class CollectionItemSource
{
    public int Id { get; set; }
}

public class CollectionItemDest
{
    public int Id { get; set; }
}

public class NullCollectionSource
{
    public List<CollectionItemSource>? Items { get; set; }
}

public class NullCollectionDest
{
    public List<CollectionItemDest> Items { get; set; } = new();
}

public class NullCollectionProfile : Profile
{
    public NullCollectionProfile()
    {
        CreateMap<CollectionItemSource, CollectionItemDest>();
        CreateMap<NullCollectionSource, NullCollectionDest>();
    }
}

// Null Array
public class NullArraySource
{
    public int[]? Values { get; set; }
}

public class NullArrayDest
{
    public int[] Values { get; set; } = Array.Empty<int>();
}

public class NullArrayProfile : Profile
{
    public NullArrayProfile()
    {
        CreateMap<NullArraySource, NullArrayDest>();
    }
}

// Null Chain
public class ChainInner
{
    public int Value { get; set; }
}

public class ChainSource
{
    public ChainInner? Inner { get; set; }
}

public class ChainDest
{
    public int InnerValue { get; set; }
}

public class NullChainProfile : Profile
{
    public NullChainProfile()
    {
        CreateMap<ChainSource, ChainDest>()
            .ForMember(d => d.InnerValue, opt => opt.MapFrom(s => s.Inner != null ? s.Inner.Value : 0));
    }
}

// Nullable Bool Converter
public class NullableBoolSource
{
    public bool? IsFooBarred { get; set; }
}

public class NullableBoolDest
{
    public string IsFooBarred { get; set; } = string.Empty;
}

public class NullableConverterProfile : Profile
{
    public NullableConverterProfile()
    {
        CreateMap<NullableBoolSource, NullableBoolDest>()
            .ConvertUsing(new NullableBoolToLabelConverter());
    }
}

public class NullableBoolToLabelConverter : ITypeConverter<NullableBoolSource, NullableBoolDest>
{
    public NullableBoolDest Convert(NullableBoolSource source, NullableBoolDest destination, ResolutionContext context)
    {
        string label;
        if (source.IsFooBarred.HasValue)
        {
            label = source.IsFooBarred.Value ? "Yes" : "No";
        }
        else
        {
            label = "(n/a)";
        }

        return new NullableBoolDest { IsFooBarred = label };
    }
}

#endregion
