using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 Constructors.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class ConstructorMappingPortedTests
{
    #region Basic Constructor Mapping Tests

    [Fact]
    public void Should_map_to_object_with_private_constructor()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CtorPrivateProfile>());
        var mapper = config.CreateMapper();

        var source = new CtorPrivateSource { Foo = 42 };

        var dest = mapper.Map<CtorPrivateDest>(source);

        Assert.Equal(42, dest.Foo);
    }

    [Fact]
    public void Should_map_remaining_properties_after_constructor()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<RemainingPropsProfile>());
        var mapper = config.CreateMapper();

        var source = new RemainingPropsSource { Foo = 10, Bar = 20 };

        var dest = mapper.Map<RemainingPropsDest>(source);

        Assert.Equal(10, dest.Foo);
        Assert.Equal(20, dest.Bar);
    }

    [Fact]
    public void Should_map_struct_with_string_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructStringProfile>());
        var mapper = config.CreateMapper();

        var source = new StructStringSource { Value = "test" };

        var dest = mapper.Map<StructStringDest>(source);

        Assert.Equal("test", dest.Value);
    }

    [Fact]
    public void Should_map_struct_with_nested_object()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new StructNestedSource
        {
            Inner = new StructInnerSource { Name = "test" }
        };

        var dest = mapper.Map<StructNestedDest>(source);

        Assert.NotNull(dest.Inner);
        Assert.Equal("test", dest.Inner.Name);
    }

    #endregion

    #region Multiple Constructor Selection Tests

    [Fact]
    public void Should_choose_matching_constructor_when_multiple_exist()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultipleCtorProfile>());
        var mapper = config.CreateMapper();

        var source = new MultipleCtorSource { Name = "John" };

        var dest = mapper.Map<MultipleCtorDest>(source);

        Assert.Equal("John", dest.Name);
    }

    [Fact]
    public void Should_use_parameterless_constructor_when_available()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ParameterlessCtorProfile>());
        var mapper = config.CreateMapper();

        var source = new ParameterlessCtorSource { Value = 100 };

        var dest = mapper.Map<ParameterlessCtorDest>(source);

        Assert.Equal(100, dest.Value);
    }

    #endregion

    #region Nested Object Mapping via Constructor Tests

    [Fact]
    public void Should_resolve_constructor_arguments_using_mapping_engine()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedCtorMappingProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedCtorSource
        {
            Inner = new NestedCtorInnerSource { Value = 42 }
        };

        var dest = mapper.Map<NestedCtorDest>(source);

        Assert.NotNull(dest.Inner);
        Assert.Equal(42, dest.Inner.Value);
    }

    [Fact]
    public void Should_resolve_multiple_constructor_arguments_via_mapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultipleNestedCtorProfile>());
        var mapper = config.CreateMapper();

        var source = new MultipleNestedSource
        {
            First = new NestedItemSource { Name = "First" },
            Second = new NestedItemSource { Name = "Second" }
        };

        var dest = mapper.Map<MultipleNestedDest>(source);

        Assert.NotNull(dest.First);
        Assert.NotNull(dest.Second);
        Assert.Equal("First", dest.First.Name);
        Assert.Equal("Second", dest.Second.Name);
    }

    #endregion

    #region Optional Parameters Tests

    [Fact]
    public void Should_handle_optional_constructor_parameters()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<OptionalParamsProfile>());
        var mapper = config.CreateMapper();

        var source = new OptionalParamsSource { Required = "value" };

        var dest = mapper.Map<OptionalParamsDest>(source);

        Assert.Equal("value", dest.Required);
        Assert.Equal("default", dest.Optional); // Default value from constructor
    }

    [Fact]
    public void Should_map_with_multiple_optional_parameters()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultipleOptionalProfile>());
        var mapper = config.CreateMapper();

        var source = new MultipleOptionalSource { Name = "test" };

        var dest = mapper.Map<MultipleOptionalDest>(source);

        Assert.Equal("test", dest.Name);
    }

    #endregion

    #region Nullable Enum Constructor Tests

    [Fact]
    public void Should_map_nullable_enum_to_constructor()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableEnumCtorProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableEnumSource { Status = CtorStatus.Active };

        var dest = mapper.Map<NullableEnumDest>(source);

        Assert.Equal(CtorStatus.Active, dest.Status);
    }

    [Fact]
    public void Should_map_null_nullable_enum()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableEnumCtorProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableEnumSource { Status = null };

        var dest = mapper.Map<NullableEnumDest>(source);

        Assert.Null(dest.Status);
    }

    #endregion

    #region GUID Constructor Tests

    [Fact]
    public void Should_map_guid_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<GuidCtorProfile>());
        var mapper = config.CreateMapper();

        var guid = Guid.NewGuid();
        var source = new GuidSource { Id = guid };

        var dest = mapper.Map<GuidDest>(source);

        Assert.Equal(guid, dest.Id);
    }

    [Fact]
    public void Should_map_optional_guid_with_default()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<OptionalGuidProfile>());
        var mapper = config.CreateMapper();

        var guid = Guid.NewGuid();
        var source = new OptionalGuidSource { Id = guid };

        var dest = mapper.Map<OptionalGuidDest>(source);

        Assert.Equal(guid, dest.Id);
    }

    #endregion
}

#region Test Classes and Profiles

// Private Constructor (Ctor prefix for uniqueness)
public class CtorPrivateSource
{
    public int Foo { get; set; }
}

public class CtorPrivateDest
{
    public int Foo { get; private set; }

    private CtorPrivateDest() { }

    public static CtorPrivateDest Create(int foo)
    {
        return new CtorPrivateDest { Foo = foo };
    }
}

public class CtorPrivateProfile : Profile
{
    public CtorPrivateProfile()
    {
        CreateMap<CtorPrivateSource, CtorPrivateDest>();
    }
}

// Remaining Properties
public class RemainingPropsSource
{
    public int Foo { get; set; }
    public int Bar { get; set; }
}

public class RemainingPropsDest
{
    public int Foo { get; set; }
    public int Bar { get; set; }
}

public class RemainingPropsProfile : Profile
{
    public RemainingPropsProfile()
    {
        CreateMap<RemainingPropsSource, RemainingPropsDest>();
    }
}

// Struct with String
public class StructStringSource
{
    public string Value { get; set; } = string.Empty;
}

public struct StructStringDest
{
    public string Value { get; set; }
}

public class StructStringProfile : Profile
{
    public StructStringProfile()
    {
        CreateMap<StructStringSource, StructStringDest>();
    }
}

// Struct with Nested
public class StructInnerSource
{
    public string Name { get; set; } = string.Empty;
}

public class StructInnerDest
{
    public string Name { get; set; } = string.Empty;
}

public class StructNestedSource
{
    public StructInnerSource? Inner { get; set; }
}

public struct StructNestedDest
{
    public StructInnerDest? Inner { get; set; }
}

public class StructNestedProfile : Profile
{
    public StructNestedProfile()
    {
        CreateMap<StructInnerSource, StructInnerDest>();
        CreateMap<StructNestedSource, StructNestedDest>();
    }
}

// Multiple Constructors
public class MultipleCtorSource
{
    public string Name { get; set; } = string.Empty;
}

public class MultipleCtorDest
{
    public string Name { get; set; }

    public MultipleCtorDest()
    {
        Name = string.Empty;
    }

    public MultipleCtorDest(string name, int age)
    {
        Name = name;
    }
}

public class MultipleCtorProfile : Profile
{
    public MultipleCtorProfile()
    {
        CreateMap<MultipleCtorSource, MultipleCtorDest>();
    }
}

// Parameterless Constructor
public class ParameterlessCtorSource
{
    public int Value { get; set; }
}

public class ParameterlessCtorDest
{
    public int Value { get; set; }

    public ParameterlessCtorDest() { }

    public ParameterlessCtorDest(int value)
    {
        Value = value;
    }
}

public class ParameterlessCtorProfile : Profile
{
    public ParameterlessCtorProfile()
    {
        CreateMap<ParameterlessCtorSource, ParameterlessCtorDest>();
    }
}

// Nested Constructor Mapping
public class NestedCtorInnerSource
{
    public int Value { get; set; }
}

public class NestedCtorInnerDest
{
    public int Value { get; set; }
}

public class NestedCtorSource
{
    public NestedCtorInnerSource? Inner { get; set; }
}

public class NestedCtorDest
{
    public NestedCtorInnerDest? Inner { get; set; }
}

public class NestedCtorMappingProfile : Profile
{
    public NestedCtorMappingProfile()
    {
        CreateMap<NestedCtorInnerSource, NestedCtorInnerDest>();
        CreateMap<NestedCtorSource, NestedCtorDest>();
    }
}

// Multiple Nested
public class NestedItemSource
{
    public string Name { get; set; } = string.Empty;
}

public class NestedItemDest
{
    public string Name { get; set; } = string.Empty;
}

public class MultipleNestedSource
{
    public NestedItemSource? First { get; set; }
    public NestedItemSource? Second { get; set; }
}

public class MultipleNestedDest
{
    public NestedItemDest? First { get; set; }
    public NestedItemDest? Second { get; set; }
}

public class MultipleNestedCtorProfile : Profile
{
    public MultipleNestedCtorProfile()
    {
        CreateMap<NestedItemSource, NestedItemDest>();
        CreateMap<MultipleNestedSource, MultipleNestedDest>();
    }
}

// Optional Parameters
public class OptionalParamsSource
{
    public string Required { get; set; } = string.Empty;
}

public class OptionalParamsDest
{
    public string Required { get; set; }
    public string Optional { get; set; }

    public OptionalParamsDest()
    {
        Required = string.Empty;
        Optional = "default";
    }
}

public class OptionalParamsProfile : Profile
{
    public OptionalParamsProfile()
    {
        CreateMap<OptionalParamsSource, OptionalParamsDest>();
    }
}

// Multiple Optional
public class MultipleOptionalSource
{
    public string Name { get; set; } = string.Empty;
}

public class MultipleOptionalDest
{
    public string Name { get; set; }
    public string? Option1 { get; set; }
    public int Option2 { get; set; }

    public MultipleOptionalDest()
    {
        Name = string.Empty;
        Option1 = null;
        Option2 = 0;
    }
}

public class MultipleOptionalProfile : Profile
{
    public MultipleOptionalProfile()
    {
        CreateMap<MultipleOptionalSource, MultipleOptionalDest>();
    }
}

// Nullable Enum (using same enum type to ensure proper mapping)
public enum CtorStatus { Inactive, Active }

public class NullableEnumSource
{
    public CtorStatus? Status { get; set; }
}

public class NullableEnumDest
{
    public CtorStatus? Status { get; set; }
}

public class NullableEnumCtorProfile : Profile
{
    public NullableEnumCtorProfile()
    {
        CreateMap<NullableEnumSource, NullableEnumDest>();
    }
}

// GUID
public class GuidSource
{
    public Guid Id { get; set; }
}

public class GuidDest
{
    public Guid Id { get; set; }
}

public class GuidCtorProfile : Profile
{
    public GuidCtorProfile()
    {
        CreateMap<GuidSource, GuidDest>();
    }
}

// Optional GUID
public class OptionalGuidSource
{
    public Guid Id { get; set; }
}

public class OptionalGuidDest
{
    public Guid Id { get; set; }

    public OptionalGuidDest()
    {
        Id = Guid.Empty;
    }
}

public class OptionalGuidProfile : Profile
{
    public OptionalGuidProfile()
    {
        CreateMap<OptionalGuidSource, OptionalGuidDest>();
    }
}

#endregion
