using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 related to mapping to existing destination objects
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class FillingExistingDestPortedTests
{
    #region Basic Existing Destination Tests

    [Fact]
    public void Should_map_to_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ExistingDestProfile>());
        var mapper = config.CreateMapper();

        var source = new ExistingSource { Name = "New", Value = 100 };
        var dest = new ExistingDest { Name = "Old", Value = 50 };

        var result = mapper.Map(source, dest);

        Assert.Same(dest, result);
        Assert.Equal("New", dest.Name);
        Assert.Equal(100, dest.Value);
    }

    [Fact]
    public void Should_update_existing_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ExistingDestProfile>());
        var mapper = config.CreateMapper();

        var source = new ExistingSource { Name = "Updated", Value = 999 };
        var dest = new ExistingDest { Name = "Original", Value = 1, ExtraProperty = "Keep This" };

        mapper.Map(source, dest);

        Assert.Equal("Updated", dest.Name);
        Assert.Equal(999, dest.Value);
        Assert.Equal("Keep This", dest.ExtraProperty); // Not mapped, preserved
    }

    #endregion

    #region Existing Destination with Null Source Tests

    [Fact]
    public void Should_return_existing_dest_when_source_is_null()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ExistingDestProfile>());
        var mapper = config.CreateMapper();

        ExistingSource? source = null;
        var dest = new ExistingDest { Name = "Preserved", Value = 42 };

        var result = mapper.Map(source, dest);

        Assert.Same(dest, result);
        Assert.Equal("Preserved", dest.Name);
        Assert.Equal(42, dest.Value);
    }

    #endregion

    #region Existing Destination with Nested Objects Tests

    [Fact]
    public void Should_map_nested_object_to_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedExistingSource
        {
            Name = "Parent",
            Child = new ChildExistingSource { ChildName = "NewChild" }
        };

        var existingChild = new ChildExistingDest { ChildName = "OldChild", ChildExtra = "Extra" };
        var dest = new NestedExistingDest { Name = "OldParent", Child = existingChild };

        mapper.Map(source, dest);

        Assert.Equal("Parent", dest.Name);
        Assert.NotNull(dest.Child);
        Assert.Equal("NewChild", dest.Child.ChildName);
    }

    [Fact]
    public void Should_handle_null_source_child_to_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedExistingSource { Name = "Parent", Child = null };
        var existingChild = new ChildExistingDest { ChildName = "Will Be Replaced" };
        var dest = new NestedExistingDest { Name = "OldParent", Child = existingChild };

        mapper.Map(source, dest);

        Assert.Equal("Parent", dest.Name);
        Assert.Null(dest.Child);
    }

    #endregion

    #region Existing Destination with Collections Tests

    [Fact]
    public void Should_replace_collection_in_existing_destination()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new CollectionExistingSource
        {
            Items = new List<ItemExistingSource>
            {
                new() { Id = 1, Name = "A" },
                new() { Id = 2, Name = "B" }
            }
        };

        var dest = new CollectionExistingDest
        {
            Items = new List<ItemExistingDest>
            {
                new() { Id = 99, Name = "Old" }
            }
        };

        mapper.Map(source, dest);

        Assert.Equal(2, dest.Items.Count);
        Assert.Equal(1, dest.Items[0].Id);
        Assert.Equal("B", dest.Items[1].Name);
    }

    #endregion

    #region Existing Destination with Type Conversion Tests

    [Fact]
    public void Should_convert_types_when_mapping_to_existing()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeConvExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new TypeConvExistingSource { IntValue = 42, StringValue = "123" };
        var dest = new TypeConvExistingDest { LongValue = 0, IntValue = 0 };

        mapper.Map(source, dest);

        Assert.Equal(42L, dest.LongValue);
        Assert.Equal(123, dest.IntValue);
    }

    #endregion

    #region Existing Destination with ForMember Tests

    [Fact]
    public void Should_apply_ForMember_when_mapping_to_existing()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ForMemberExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new ForMemberExistingSource { First = "John", Last = "Doe" };
        var dest = new ForMemberExistingDest { FullName = "Old Name", Extra = "Keep" };

        mapper.Map(source, dest);

        Assert.Equal("John Doe", dest.FullName);
        Assert.Equal("Keep", dest.Extra);
    }

    [Fact]
    public void Should_apply_Ignore_when_mapping_to_existing()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ForMemberExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new IgnoreExistingSource { Name = "New", Ignored = "Should Not Replace" };
        var dest = new IgnoreExistingDest { Name = "Old", Ignored = "Preserved" };

        mapper.Map(source, dest);

        Assert.Equal("New", dest.Name);
        Assert.Equal("Preserved", dest.Ignored);
    }

    #endregion

    #region Existing Destination with Converter Tests

    [Fact]
    public void Should_use_converter_when_mapping_to_existing()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ConverterExistingProfile>());
        var mapper = config.CreateMapper();

        var source = new ConverterExistingSource { Value = 42 };
        var dest = new ConverterExistingDest { Result = "Old" };

        var result = mapper.Map(source, dest);

        Assert.Same(dest, result);
        Assert.Equal("Value is 42", dest.Result);
    }

    #endregion

    #region Map Overload Tests

    [Fact]
    public void Should_work_with_generic_Map_overload()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ExistingDestProfile>());
        var mapper = config.CreateMapper();

        var source = new ExistingSource { Name = "Generic", Value = 200 };
        var dest = new ExistingDest { Name = "Old", Value = 0 };

        var result = mapper.Map<ExistingSource, ExistingDest>(source, dest);

        Assert.Same(dest, result);
        Assert.Equal("Generic", dest.Name);
        Assert.Equal(200, dest.Value);
    }

    [Fact]
    public void Should_work_with_non_generic_Map_overload()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ExistingDestProfile>());
        var mapper = config.CreateMapper();

        var source = new ExistingSource { Name = "NonGeneric", Value = 300 };
        var dest = new ExistingDest { Name = "Old", Value = 0 };

        var result = mapper.Map(source, dest, typeof(ExistingSource), typeof(ExistingDest));

        Assert.Same(dest, result);
        Assert.Equal("NonGeneric", ((ExistingDest)result).Name);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Existing Destination
public class ExistingSource
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class ExistingDest
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string ExtraProperty { get; set; } = string.Empty;
}

public class ExistingDestProfile : Profile
{
    public ExistingDestProfile()
    {
        CreateMap<ExistingSource, ExistingDest>();
    }
}

// Nested Existing
public class ChildExistingSource
{
    public string ChildName { get; set; } = string.Empty;
}

public class ChildExistingDest
{
    public string ChildName { get; set; } = string.Empty;
    public string ChildExtra { get; set; } = string.Empty;
}

public class NestedExistingSource
{
    public string Name { get; set; } = string.Empty;
    public ChildExistingSource? Child { get; set; }
}

public class NestedExistingDest
{
    public string Name { get; set; } = string.Empty;
    public ChildExistingDest? Child { get; set; }
}

public class NestedExistingProfile : Profile
{
    public NestedExistingProfile()
    {
        CreateMap<ChildExistingSource, ChildExistingDest>();
        CreateMap<NestedExistingSource, NestedExistingDest>();
    }
}

// Collection Existing
public class ItemExistingSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ItemExistingDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CollectionExistingSource
{
    public List<ItemExistingSource> Items { get; set; } = new();
}

public class CollectionExistingDest
{
    public List<ItemExistingDest> Items { get; set; } = new();
}

public class CollectionExistingProfile : Profile
{
    public CollectionExistingProfile()
    {
        CreateMap<ItemExistingSource, ItemExistingDest>();
        CreateMap<CollectionExistingSource, CollectionExistingDest>();
    }
}

// Type Conversion Existing
public class TypeConvExistingSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
}

public class TypeConvExistingDest
{
    public long LongValue { get; set; }
    public int IntValue { get; set; }
}

public class TypeConvExistingProfile : Profile
{
    public TypeConvExistingProfile()
    {
        CreateMap<TypeConvExistingSource, TypeConvExistingDest>()
            .ForMember(d => d.LongValue, opt => opt.MapFrom(s => (long)s.IntValue))
            .ForMember(d => d.IntValue, opt => opt.MapFrom(s => int.Parse(s.StringValue)));
    }
}

// ForMember Existing
public class ForMemberExistingSource
{
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
}

public class ForMemberExistingDest
{
    public string FullName { get; set; } = string.Empty;
    public string Extra { get; set; } = string.Empty;
}

public class IgnoreExistingSource
{
    public string Name { get; set; } = string.Empty;
    public string Ignored { get; set; } = string.Empty;
}

public class IgnoreExistingDest
{
    public string Name { get; set; } = string.Empty;
    public string Ignored { get; set; } = string.Empty;
}

public class ForMemberExistingProfile : Profile
{
    public ForMemberExistingProfile()
    {
        CreateMap<ForMemberExistingSource, ForMemberExistingDest>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.First + " " + s.Last));

        CreateMap<IgnoreExistingSource, IgnoreExistingDest>()
            .ForMember(d => d.Ignored, opt => opt.Ignore());
    }
}

// Converter Existing
public class ConverterExistingSource
{
    public int Value { get; set; }
}

public class ConverterExistingDest
{
    public string Result { get; set; } = string.Empty;
}

public class ExistingDestConverter : ITypeConverter<ConverterExistingSource, ConverterExistingDest>
{
    public ConverterExistingDest Convert(ConverterExistingSource source, ConverterExistingDest destination, ResolutionContext context)
    {
        destination ??= new ConverterExistingDest();
        destination.Result = $"Value is {source.Value}";
        return destination;
    }
}

public class ConverterExistingProfile : Profile
{
    public ConverterExistingProfile()
    {
        CreateMap<ConverterExistingSource, ConverterExistingDest>()
            .ConvertUsing(new ExistingDestConverter());
    }
}

#endregion
