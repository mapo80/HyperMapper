using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 MemberResolution.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class MemberResolutionPortedTests
{
    #region Simple Member Resolution Tests

    [Fact]
    public void Should_map_matching_properties_by_name()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MemberResolutionProfile>());
        var mapper = config.CreateMapper();

        var source = new MemberSource { FirstName = "John", LastName = "Doe", Age = 30 };
        var dest = mapper.Map<MemberDest>(source);

        Assert.Equal("John", dest.FirstName);
        Assert.Equal("Doe", dest.LastName);
        Assert.Equal(30, dest.Age);
    }

    [Fact]
    public void Should_ignore_case_in_property_matching()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CaseMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new CaseSource { firstname = "John", LASTNAME = "Doe" };
        var dest = mapper.Map<CaseDest>(source);

        Assert.Equal("John", dest.FirstName);
        Assert.Equal("Doe", dest.LastName);
    }

    #endregion

    #region Nested Member Resolution Tests

    [Fact]
    public void Should_map_nested_properties_automatically()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedMemberSource
        {
            Name = "Test",
            Inner = new InnerMemberSource { Value = 42 }
        };

        var dest = mapper.Map<NestedMemberDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.NotNull(dest.Inner);
        Assert.Equal(42, dest.Inner.Value);
    }

    [Fact]
    public void Should_handle_null_nested_members()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedMemberSource { Name = "Test", Inner = null };
        var dest = mapper.Map<NestedMemberDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Null(dest.Inner);
    }

    #endregion

    #region Custom Member Resolution Tests

    [Fact]
    public void Should_use_MapFrom_for_custom_resolution()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CustomResolutionProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomResSource { First = "John", Last = "Doe" };
        var dest = mapper.Map<CustomResDest>(source);

        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void Should_use_MapFrom_with_expression()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CustomResolutionProfile>());
        var mapper = config.CreateMapper();

        var source = new ExpressionSource { Value = 10 };
        var dest = mapper.Map<ExpressionDest>(source);

        Assert.Equal(20, dest.DoubleValue);
    }

    [Fact]
    public void Should_use_MapFrom_with_nested_expression()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CustomResolutionProfile>());
        var mapper = config.CreateMapper();

        var source = new MemberDeepSource
        {
            Level1 = new MemberLevel1Source
            {
                Level2 = new MemberLevel2Source { DeepValue = "Found" }
            }
        };

        var dest = mapper.Map<MemberDeepDest>(source);

        Assert.Equal("Found", dest.ExtractedValue);
    }

    #endregion

    #region Collection Member Resolution Tests

    [Fact]
    public void Should_map_collection_members()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new CollectionMemberSource
        {
            Items = new List<MemberItemSource>
            {
                new() { Id = 1, Name = "A" },
                new() { Id = 2, Name = "B" }
            }
        };

        var dest = mapper.Map<CollectionMemberDest>(source);

        Assert.Equal(2, dest.Items.Count);
        Assert.Equal(1, dest.Items[0].Id);
        Assert.Equal("B", dest.Items[1].Name);
    }

    [Fact]
    public void Should_map_array_members()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new ArrayMemberSource
        {
            Values = new[] { 1, 2, 3, 4, 5 }
        };

        var dest = mapper.Map<ArrayMemberDest>(source);

        Assert.Equal(5, dest.Values.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, dest.Values);
    }

    #endregion

    #region Property Type Conversion Tests

    [Fact]
    public void Should_convert_compatible_types_automatically()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeConversionMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new MemberTypeConvSource { IntValue = 42, DoubleValue = 3.14 };
        var dest = mapper.Map<MemberTypeConvDest>(source);

        Assert.Equal(42, dest.IntValue);
        Assert.Equal(3, dest.DoubleValue); // double to int truncates
    }

    [Fact]
    public void Should_convert_to_string_automatically()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeConversionMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new MemberToStringSource { Value = 123 };
        var dest = mapper.Map<MemberToStringDest>(source);

        Assert.Equal("123", dest.Value);
    }

    [Fact]
    public void Should_convert_nullable_to_non_nullable()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeConversionMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new MemberNullableToNonSource { Value = 42 };
        var dest = mapper.Map<MemberNullableToNonDest>(source);

        Assert.Equal(42, dest.Value);
    }

    #endregion
}

#region Test Classes and Profiles

// Simple Member Resolution
public class MemberSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class MemberDest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class MemberResolutionProfile : Profile
{
    public MemberResolutionProfile()
    {
        CreateMap<MemberSource, MemberDest>();
    }
}

// Case Sensitivity
public class CaseSource
{
    public string firstname { get; set; } = string.Empty;
    public string LASTNAME { get; set; } = string.Empty;
}

public class CaseDest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class CaseMemberProfile : Profile
{
    public CaseMemberProfile()
    {
        CreateMap<CaseSource, CaseDest>()
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.firstname))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.LASTNAME));
    }
}

// Nested Members
public class InnerMemberSource
{
    public int Value { get; set; }
}

public class InnerMemberDest
{
    public int Value { get; set; }
}

public class NestedMemberSource
{
    public string Name { get; set; } = string.Empty;
    public InnerMemberSource? Inner { get; set; }
}

public class NestedMemberDest
{
    public string Name { get; set; } = string.Empty;
    public InnerMemberDest? Inner { get; set; }
}

public class NestedMemberProfile : Profile
{
    public NestedMemberProfile()
    {
        CreateMap<InnerMemberSource, InnerMemberDest>();
        CreateMap<NestedMemberSource, NestedMemberDest>();
    }
}

// Custom Resolution
public class CustomResSource
{
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
}

public class CustomResDest
{
    public string FullName { get; set; } = string.Empty;
}

public class ExpressionSource
{
    public int Value { get; set; }
}

public class ExpressionDest
{
    public int DoubleValue { get; set; }
}

public class MemberLevel2Source
{
    public string DeepValue { get; set; } = string.Empty;
}

public class MemberLevel1Source
{
    public MemberLevel2Source? Level2 { get; set; }
}

public class MemberDeepSource
{
    public MemberLevel1Source? Level1 { get; set; }
}

public class MemberDeepDest
{
    public string ExtractedValue { get; set; } = string.Empty;
}

public class CustomResolutionProfile : Profile
{
    public CustomResolutionProfile()
    {
        CreateMap<CustomResSource, CustomResDest>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.First + " " + s.Last));

        CreateMap<ExpressionSource, ExpressionDest>()
            .ForMember(d => d.DoubleValue, opt => opt.MapFrom(s => s.Value * 2));

        CreateMap<MemberDeepSource, MemberDeepDest>()
            .ForMember(d => d.ExtractedValue, opt => opt.MapFrom(s => s.Level1 != null && s.Level1.Level2 != null ? s.Level1.Level2.DeepValue : string.Empty));
    }
}

// Collection Members
public class MemberItemSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MemberItemDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CollectionMemberSource
{
    public List<MemberItemSource> Items { get; set; } = new();
}

public class CollectionMemberDest
{
    public List<MemberItemDest> Items { get; set; } = new();
}

public class ArrayMemberSource
{
    public int[] Values { get; set; } = Array.Empty<int>();
}

public class ArrayMemberDest
{
    public int[] Values { get; set; } = Array.Empty<int>();
}

public class CollectionMemberProfile : Profile
{
    public CollectionMemberProfile()
    {
        CreateMap<MemberItemSource, MemberItemDest>();
        CreateMap<CollectionMemberSource, CollectionMemberDest>();
        CreateMap<ArrayMemberSource, ArrayMemberDest>();
    }
}

// Type Conversion Members
public class MemberTypeConvSource
{
    public int IntValue { get; set; }
    public double DoubleValue { get; set; }
}

public class MemberTypeConvDest
{
    public long IntValue { get; set; }
    public int DoubleValue { get; set; }
}

public class MemberToStringSource
{
    public int Value { get; set; }
}

public class MemberToStringDest
{
    public string Value { get; set; } = string.Empty;
}

public class MemberNullableToNonSource
{
    public int? Value { get; set; }
}

public class MemberNullableToNonDest
{
    public int Value { get; set; }
}

public class TypeConversionMemberProfile : Profile
{
    public TypeConversionMemberProfile()
    {
        CreateMap<MemberTypeConvSource, MemberTypeConvDest>();
        CreateMap<MemberToStringSource, MemberToStringDest>();
        CreateMap<MemberNullableToNonSource, MemberNullableToNonDest>();
    }
}

#endregion
