using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 General.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class GeneralMappingPortedTests
{
    #region Null Source Mapping Tests

    [Fact]
    public void When_mapping_null_source_Should_return_null()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<GeneralProfile>());
        var mapper = config.CreateMapper();

        GeneralSource? source = null;
        var dest = mapper.Map<GeneralDest>(source!);

        Assert.Null(dest);
    }

    [Fact]
    public void When_mapping_null_source_to_existing_Should_return_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<GeneralProfile>());
        var mapper = config.CreateMapper();

        GeneralSource? source = null;
        var dest = new GeneralDest { Value = 42 };

        var result = mapper.Map(source, dest);

        Assert.Same(dest, result);
        Assert.Equal(42, result.Value);
    }

    #endregion

    #region ToString Conversion Tests

    [Fact]
    public void When_source_has_object_type_should_convert_via_ToString()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ToStringProfile>());
        var mapper = config.CreateMapper();

        var source = new ToStringSource { Number = 123 };
        var dest = mapper.Map<ToStringDest>(source);

        Assert.Equal("123", dest.Number);
    }

    [Fact]
    public void When_mapping_int_to_string_should_use_ToString()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ToStringProfile>());
        var mapper = config.CreateMapper();

        var source = new IntSource { Id = 42 };
        var dest = mapper.Map<StringDest>(source);

        Assert.Equal("42", dest.Id);
    }

    #endregion

    #region Array Property Mapping Tests

    [Fact]
    public void When_source_has_array_property_should_map_array()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ArrayPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new ArrayPropertySource
        {
            Values = new[] { 1, 2, 3 }
        };

        var dest = mapper.Map<ArrayPropertyDest>(source);

        Assert.Equal(3, dest.Values.Length);
        Assert.Equal(new[] { 1, 2, 3 }, dest.Values);
    }

    [Fact]
    public void When_source_has_null_array_should_map_to_empty_or_null()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ArrayPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new ArrayPropertySource { Values = null };

        var dest = mapper.Map<ArrayPropertyDest>(source);

        Assert.NotNull(dest.Values);
        Assert.Empty(dest.Values);
    }

    #endregion

    #region Array Of Objects Mapping Tests

    [Fact]
    public void When_source_has_array_of_objects_should_map_each()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ObjectArrayProfile>());
        var mapper = config.CreateMapper();

        var source = new ObjectArraySource
        {
            Items = new[]
            {
                new ItemSource { Name = "Item1" },
                new ItemSource { Name = "Item2" }
            }
        };

        var dest = mapper.Map<ObjectArrayDest>(source);

        Assert.Equal(2, dest.Items.Length);
        Assert.Equal("Item1", dest.Items[0].Name);
        Assert.Equal("Item2", dest.Items[1].Name);
    }

    #endregion

    #region List To Array Conversion Tests

    [Fact]
    public void When_mapping_list_to_array_should_convert()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ListToArrayProfile>());
        var mapper = config.CreateMapper();

        var source = new GeneralListSource
        {
            Items = new List<string> { "a", "b", "c" }
        };

        var dest = mapper.Map<GeneralArrayDest>(source);

        Assert.Equal(3, dest.Items.Length);
        Assert.Equal(new[] { "a", "b", "c" }, dest.Items);
    }

    #endregion

    #region Nullable Type Mapping Tests

    [Fact]
    public void When_mapping_nullable_to_non_nullable_with_value_should_unwrap()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableSource { Value = 42 };
        var dest = mapper.Map<NonNullableDest>(source);

        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void When_mapping_nullable_to_non_nullable_null_should_use_default()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableSource { Value = null };
        var dest = mapper.Map<NonNullableDest>(source);

        Assert.Equal(0, dest.Value);
    }

    [Fact]
    public void When_mapping_non_nullable_to_nullable_should_wrap()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableProfile>());
        var mapper = config.CreateMapper();

        var source = new NonNullableSource { Value = 42 };
        var dest = mapper.Map<NullableDestination>(source);

        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void When_mapping_nullable_to_nullable_should_preserve()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableSource { Value = 42 };
        var dest = mapper.Map<NullableDestination>(source);

        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void When_mapping_nullable_null_to_nullable_should_be_null()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableSource { Value = null };
        var dest = mapper.Map<NullableDestination>(source);

        Assert.Null(dest.Value);
    }

    #endregion

    #region Private Constructor Tests

    [Fact]
    public void When_destination_has_private_parameterless_constructor_should_map()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PrivateCtorProfile>());
        var mapper = config.CreateMapper();

        var source = new PublicSource { Value = "Test" };
        var dest = mapper.Map<PrivateCtorDest>(source);

        Assert.Equal("Test", dest.Value);
    }

    #endregion

    #region Missing Property Mapping Tests

    [Fact]
    public void When_destination_has_extra_properties_should_map_matched_only()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MissingPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new PartialSource { Id = 1 };
        var dest = mapper.Map<ExtendedDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Null(dest.Name); // Not mapped, defaults to null
        Assert.Equal(0, dest.Count); // Not mapped, defaults to 0
    }

    [Fact]
    public void When_source_has_extra_properties_should_ignore()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MissingPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new ExtendedSource { Id = 1, Name = "Test", Extra = "Ignored" };
        var dest = mapper.Map<MinimalDest>(source);

        Assert.Equal(1, dest.Id);
    }

    #endregion

    #region Complex Nested Mapping Tests

    [Fact]
    public void When_mapping_complex_nested_structure_should_map_all()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new ComplexSource
        {
            Id = 1,
            Name = "Root",
            Child = new ComplexChildSource
            {
                Value = 42,
                GrandChild = new ComplexGrandChildSource
                {
                    Description = "Deep"
                }
            }
        };

        var dest = mapper.Map<ComplexDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Root", dest.Name);
        Assert.NotNull(dest.Child);
        Assert.Equal(42, dest.Child.Value);
        Assert.NotNull(dest.Child.GrandChild);
        Assert.Equal("Deep", dest.Child.GrandChild.Description);
    }

    #endregion
}

#region Test Classes and Profiles

// General
public class GeneralSource { public int Value { get; set; } }
public class GeneralDest { public int Value { get; set; } }

public class GeneralProfile : Profile
{
    public GeneralProfile()
    {
        CreateMap<GeneralSource, GeneralDest>();
    }
}

// ToString Conversion
public class ToStringSource { public int Number { get; set; } }
public class ToStringDest { public string Number { get; set; } = string.Empty; }
public class IntSource { public int Id { get; set; } }
public class StringDest { public string Id { get; set; } = string.Empty; }

public class ToStringProfile : Profile
{
    public ToStringProfile()
    {
        CreateMap<ToStringSource, ToStringDest>();
        CreateMap<IntSource, StringDest>();
    }
}

// Array Property
public class ArrayPropertySource { public int[]? Values { get; set; } }
public class ArrayPropertyDest { public int[] Values { get; set; } = Array.Empty<int>(); }

public class ArrayPropertyProfile : Profile
{
    public ArrayPropertyProfile()
    {
        CreateMap<ArrayPropertySource, ArrayPropertyDest>();
    }
}

// Object Array
public class ItemSource { public string Name { get; set; } = string.Empty; }
public class ItemDest { public string Name { get; set; } = string.Empty; }
public class ObjectArraySource { public ItemSource[] Items { get; set; } = Array.Empty<ItemSource>(); }
public class ObjectArrayDest { public ItemDest[] Items { get; set; } = Array.Empty<ItemDest>(); }

public class ObjectArrayProfile : Profile
{
    public ObjectArrayProfile()
    {
        CreateMap<ItemSource, ItemDest>();
        CreateMap<ObjectArraySource, ObjectArrayDest>();
    }
}

// List to Array
public class GeneralListSource { public List<string> Items { get; set; } = new(); }
public class GeneralArrayDest { public string[] Items { get; set; } = Array.Empty<string>(); }

public class ListToArrayProfile : Profile
{
    public ListToArrayProfile()
    {
        CreateMap<GeneralListSource, GeneralArrayDest>();
    }
}

// Nullable Types
public class NullableSource { public int? Value { get; set; } }
public class NonNullableDest { public int Value { get; set; } }
public class NonNullableSource { public int Value { get; set; } }
public class NullableDestination { public int? Value { get; set; } }

public class NullableProfile : Profile
{
    public NullableProfile()
    {
        CreateMap<NullableSource, NonNullableDest>();
        CreateMap<NonNullableSource, NullableDestination>();
        CreateMap<NullableSource, NullableDestination>();
    }
}

// Private Constructor
public class PublicSource { public string Value { get; set; } = string.Empty; }

public class PrivateCtorDest
{
    public string Value { get; set; } = string.Empty;
    private PrivateCtorDest() { }
}

public class PrivateCtorProfile : Profile
{
    public PrivateCtorProfile()
    {
        CreateMap<PublicSource, PrivateCtorDest>();
    }
}

// Missing Property
public class PartialSource { public int Id { get; set; } }
public class ExtendedDest { public int Id { get; set; } public string? Name { get; set; } public int Count { get; set; } }
public class ExtendedSource { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Extra { get; set; } = string.Empty; }
public class MinimalDest { public int Id { get; set; } }

public class MissingPropertyProfile : Profile
{
    public MissingPropertyProfile()
    {
        CreateMap<PartialSource, ExtendedDest>();
        CreateMap<ExtendedSource, MinimalDest>();
    }
}

// Complex Nested
public class ComplexSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComplexChildSource? Child { get; set; }
}

public class ComplexChildSource
{
    public int Value { get; set; }
    public ComplexGrandChildSource? GrandChild { get; set; }
}

public class ComplexGrandChildSource
{
    public string Description { get; set; } = string.Empty;
}

public class ComplexDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComplexChildDest? Child { get; set; }
}

public class ComplexChildDest
{
    public int Value { get; set; }
    public ComplexGrandChildDest? GrandChild { get; set; }
}

public class ComplexGrandChildDest
{
    public string Description { get; set; } = string.Empty;
}

public class ComplexNestedProfile : Profile
{
    public ComplexNestedProfile()
    {
        CreateMap<ComplexGrandChildSource, ComplexGrandChildDest>();
        CreateMap<ComplexChildSource, ComplexChildDest>();
        CreateMap<ComplexSource, ComplexDest>();
    }
}

#endregion
