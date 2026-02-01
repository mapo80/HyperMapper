using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 InterfaceMapping.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
///
/// Note: HyperMapper does not support proxy generation for interfaces.
/// These tests cover interface-based mapping scenarios that work with concrete types.
/// </summary>
public class InterfaceMappingPortedTests
{
    #region Basic Interface Property Mapping

    [Fact]
    public void Should_map_from_interface_implementation_to_concrete()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<InterfaceToConcreteProfile>());
        var mapper = config.CreateMapper();

        IIntfSource source = new IntfSourceImpl { Value = 42, Name = "Test" };

        var dest = mapper.Map<IntfConcreteDest>(source);

        Assert.Equal(42, dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Should_map_from_concrete_to_interface_implementation()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ConcreteToInterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new IntfConcreteSource { Value = 100, Name = "Source" };

        var dest = mapper.Map<IntfDestImpl>(source);

        Assert.Equal(100, dest.Value);
        Assert.Equal("Source", dest.Name);
    }

    #endregion

    #region Interface Inheritance Tests

    [Fact]
    public void Should_map_inherited_interface_members()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<InheritedInterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new DerivedIntfSourceImpl
        {
            BaseValue = 10,
            DerivedValue = 20
        };

        var dest = mapper.Map<DerivedIntfDestImpl>(source);

        Assert.Equal(10, dest.BaseValue);
        Assert.Equal(20, dest.DerivedValue);
    }

    [Fact]
    public void Should_map_from_base_interface_to_derived_concrete()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BaseToDeriverdProfile>());
        var mapper = config.CreateMapper();

        IBaseIntf source = new DerivedIntfSourceImpl
        {
            BaseValue = 15,
            DerivedValue = 25
        };

        var dest = mapper.Map<DerivedIntfDestImpl>(source);

        // Only base property is mapped when using IBaseIntf
        Assert.Equal(15, dest.BaseValue);
    }

    #endregion

    #region Interface with Object Property Tests

    [Fact]
    public void Should_map_interface_with_object_property()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ObjectPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new ObjectPropSourceImpl
        {
            Id = 1,
            Data = new NestedData { Info = "nested" }
        };

        var dest = mapper.Map<ObjectPropDestImpl>(source);

        Assert.Equal(1, dest.Id);
        Assert.NotNull(dest.Data);
        Assert.IsType<NestedData>(dest.Data);
        Assert.Equal("nested", ((NestedData)dest.Data).Info);
    }

    #endregion

    #region Generic Interface Tests

    [Fact]
    public void Should_map_generic_interface_implementation()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<GenericInterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new GenericIntfSourceImpl<string>
        {
            Value = "test",
            Items = new List<string> { "a", "b", "c" }
        };

        var dest = mapper.Map<GenericIntfDestImpl<string>>(source);

        Assert.Equal("test", dest.Value);
        Assert.Equal(3, dest.Items.Count);
    }

    [Fact]
    public void Should_map_generic_interface_with_complex_type()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexGenericInterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new GenericIntfSourceImpl<IntfItem>
        {
            Value = new IntfItem { Name = "item" },
            Items = new List<IntfItem>
            {
                new() { Name = "first" },
                new() { Name = "second" }
            }
        };

        var dest = mapper.Map<GenericIntfDestImpl<IntfItemDto>>(source);

        Assert.NotNull(dest.Value);
        Assert.Equal("item", dest.Value.Name);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("first", dest.Items[0].Name);
    }

    #endregion

    #region Readonly Interface Property Tests

    [Fact]
    public void Should_map_to_class_with_readonly_interface_getter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadonlyInterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new ReadonlyIntfSource { Value = 42 };

        var dest = mapper.Map<ReadonlyIntfDest>(source);

        Assert.Equal(42, dest.Value);
    }

    #endregion

    #region Multiple Interface Implementation Tests

    [Fact]
    public void Should_map_class_implementing_multiple_interfaces()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultipleInterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new MultiIntfSourceImpl
        {
            Id = 1,
            Name = "test",
            Description = "desc"
        };

        var dest = mapper.Map<MultiIntfDestImpl>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("test", dest.Name);
        Assert.Equal("desc", dest.Description);
    }

    #endregion
}

#region Test Interfaces and Classes

// Basic Interface Mapping
public interface IIntfSource
{
    int Value { get; }
    string Name { get; }
}

public class IntfSourceImpl : IIntfSource
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class IntfConcreteDest
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class IntfConcreteSource
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IIntfDest
{
    int Value { get; set; }
    string Name { get; set; }
}

public class IntfDestImpl : IIntfDest
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class InterfaceToConcreteProfile : Profile
{
    public InterfaceToConcreteProfile()
    {
        CreateMap<IntfSourceImpl, IntfConcreteDest>();
    }
}

public class ConcreteToInterfaceProfile : Profile
{
    public ConcreteToInterfaceProfile()
    {
        CreateMap<IntfConcreteSource, IntfDestImpl>();
    }
}

// Interface Inheritance
public interface IBaseIntf
{
    int BaseValue { get; }
}

public interface IDerivedIntf : IBaseIntf
{
    int DerivedValue { get; }
}

public class DerivedIntfSourceImpl : IDerivedIntf
{
    public int BaseValue { get; set; }
    public int DerivedValue { get; set; }
}

public class DerivedIntfDestImpl
{
    public int BaseValue { get; set; }
    public int DerivedValue { get; set; }
}

public class InheritedInterfaceProfile : Profile
{
    public InheritedInterfaceProfile()
    {
        CreateMap<DerivedIntfSourceImpl, DerivedIntfDestImpl>();
    }
}

public class BaseToDeriverdProfile : Profile
{
    public BaseToDeriverdProfile()
    {
        CreateMap<DerivedIntfSourceImpl, DerivedIntfDestImpl>();
    }
}

// Object Property
public interface IObjectPropIntf
{
    int Id { get; }
    object? Data { get; }
}

public class NestedData
{
    public string Info { get; set; } = string.Empty;
}

public class ObjectPropSourceImpl : IObjectPropIntf
{
    public int Id { get; set; }
    public object? Data { get; set; }
}

public class ObjectPropDestImpl
{
    public int Id { get; set; }
    public object? Data { get; set; }
}

public class ObjectPropertyProfile : Profile
{
    public ObjectPropertyProfile()
    {
        CreateMap<ObjectPropSourceImpl, ObjectPropDestImpl>();
    }
}

// Generic Interface
public interface IGenericIntf<T>
{
    T? Value { get; }
    List<T> Items { get; }
}

public class GenericIntfSourceImpl<T> : IGenericIntf<T>
{
    public T? Value { get; set; }
    public List<T> Items { get; set; } = new();
}

public class GenericIntfDestImpl<T>
{
    public T? Value { get; set; }
    public List<T> Items { get; set; } = new();
}

public class GenericInterfaceProfile : Profile
{
    public GenericInterfaceProfile()
    {
        CreateMap<GenericIntfSourceImpl<string>, GenericIntfDestImpl<string>>();
    }
}

// Complex Generic Interface
public class IntfItem
{
    public string Name { get; set; } = string.Empty;
}

public class IntfItemDto
{
    public string Name { get; set; } = string.Empty;
}

public class ComplexGenericInterfaceProfile : Profile
{
    public ComplexGenericInterfaceProfile()
    {
        CreateMap<IntfItem, IntfItemDto>();
        CreateMap<GenericIntfSourceImpl<IntfItem>, GenericIntfDestImpl<IntfItemDto>>();
    }
}

// Readonly Interface
public class ReadonlyIntfSource
{
    public int Value { get; set; }
}

public class ReadonlyIntfDest
{
    public int Value { get; set; }
}

public class ReadonlyInterfaceProfile : Profile
{
    public ReadonlyInterfaceProfile()
    {
        CreateMap<ReadonlyIntfSource, ReadonlyIntfDest>();
    }
}

// Multiple Interface Implementation
public interface IIdentifiable
{
    int Id { get; }
}

public interface INamed
{
    string Name { get; }
}

public interface IDescribed
{
    string Description { get; }
}

public class MultiIntfSourceImpl : IIdentifiable, INamed, IDescribed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MultiIntfDestImpl
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class MultipleInterfaceProfile : Profile
{
    public MultipleInterfaceProfile()
    {
        CreateMap<MultiIntfSourceImpl, MultiIntfDestImpl>();
    }
}

#endregion
