using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for prefix/suffix recognition ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
///
/// Note: HyperMapper uses convention-based mapping with exact name matching.
/// These tests verify standard property mapping behavior.
/// </summary>
public class PrefixMappingPortedTests
{
    #region Standard Property Mapping Tests

    [Fact]
    public void Should_map_properties_with_same_name()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StandardMappingProfile>());
        var mapper = config.CreateMapper();

        var source = new StandardSource
        {
            Name = "Test",
            Value = 42,
            Description = "Desc"
        };

        var dest = mapper.Map<StandardDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(42, dest.Value);
        Assert.Equal("Desc", dest.Description);
    }

    [Fact]
    public void Should_map_with_ForMember_for_different_names()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<DifferentNameProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithGetPrefix
        {
            GetName = "Test",
            GetValue = 100
        };

        var dest = mapper.Map<DestWithoutPrefix>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(100, dest.Value);
    }

    #endregion

    #region Nested Property Flattening Tests

    [Fact]
    public void Should_flatten_nested_property_with_ForMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PrefixFlatteningProfile>());
        var mapper = config.CreateMapper();

        var source = new PrefixSourceWithNested
        {
            Customer = new CustomerInfo
            {
                Name = "John",
                Email = "john@example.com"
            }
        };

        var dest = mapper.Map<PrefixFlattenedDest>(source);

        Assert.Equal("John", dest.CustomerName);
        Assert.Equal("john@example.com", dest.CustomerEmail);
    }

    #endregion

    #region Multiple Property Sources Tests

    [Fact]
    public void Should_combine_properties_with_ForMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CombinePropertiesProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithParts
        {
            FirstName = "John",
            LastName = "Doe",
            Street = "123 Main St",
            City = "NYC"
        };

        var dest = mapper.Map<CombinedDest>(source);

        Assert.Equal("John Doe", dest.FullName);
        Assert.Equal("123 Main St, NYC", dest.FullAddress);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void Should_map_exact_case_match()
    {
        // HyperMapper now supports case-sensitive matching with case-insensitive fallback.
        // When source has Name/NAME/name, it matches destination Name exactly first.
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CaseSensitiveProfile>());
        var mapper = config.CreateMapper();

        var source = new CaseSensitiveSource
        {
            Name = "Test",
            NAME = "UPPER",
            name = "lower"
        };

        var dest = mapper.Map<CaseSensitiveDest>(source);

        Assert.Equal("Test", dest.Name);
    }

    #endregion

    #region Boolean Property Tests

    [Fact]
    public void Should_map_boolean_properties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BooleanPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithBooleans
        {
            IsActive = true,
            HasPermission = false,
            CanEdit = true
        };

        var dest = mapper.Map<DestWithBooleans>(source);

        Assert.True(dest.IsActive);
        Assert.False(dest.HasPermission);
        Assert.True(dest.CanEdit);
    }

    #endregion

    #region Numeric Prefix Tests

    [Fact]
    public void Should_map_properties_with_numbers()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NumericPropertyProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNumbers
        {
            Value1 = 10,
            Value2 = 20,
            Item3Name = "Third"
        };

        var dest = mapper.Map<DestWithNumbers>(source);

        Assert.Equal(10, dest.Value1);
        Assert.Equal(20, dest.Value2);
        Assert.Equal("Third", dest.Item3Name);
    }

    #endregion
}

#region Test Classes and Profiles

// Standard Mapping
public class StandardSource
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class StandardDest
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class StandardMappingProfile : Profile
{
    public StandardMappingProfile()
    {
        CreateMap<StandardSource, StandardDest>();
    }
}

// Different Name with Prefix
public class SourceWithGetPrefix
{
    public string GetName { get; set; } = string.Empty;
    public int GetValue { get; set; }
}

public class DestWithoutPrefix
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DifferentNameProfile : Profile
{
    public DifferentNameProfile()
    {
        CreateMap<SourceWithGetPrefix, DestWithoutPrefix>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.GetName))
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.GetValue));
    }
}

// Flattening
public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class PrefixSourceWithNested
{
    public CustomerInfo? Customer { get; set; }
}

public class PrefixFlattenedDest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}

public class PrefixFlatteningProfile : Profile
{
    public PrefixFlatteningProfile()
    {
        CreateMap<PrefixSourceWithNested, PrefixFlattenedDest>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : string.Empty))
            .ForMember(d => d.CustomerEmail, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Email : string.Empty));
    }
}

// Combine Properties
public class SourceWithParts
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class CombinedDest
{
    public string FullName { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
}

public class CombinePropertiesProfile : Profile
{
    public CombinePropertiesProfile()
    {
        CreateMap<SourceWithParts, CombinedDest>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.FullAddress, opt => opt.MapFrom(s => $"{s.Street}, {s.City}"));
    }
}

// Case Sensitive
public class CaseSensitiveSource
{
    public string Name { get; set; } = string.Empty;
    public string NAME { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
}

public class CaseSensitiveDest
{
    public string Name { get; set; } = string.Empty;
}

public class CaseSensitiveProfile : Profile
{
    public CaseSensitiveProfile()
    {
        CreateMap<CaseSensitiveSource, CaseSensitiveDest>();
    }
}

// Boolean Properties
public class SourceWithBooleans
{
    public bool IsActive { get; set; }
    public bool HasPermission { get; set; }
    public bool CanEdit { get; set; }
}

public class DestWithBooleans
{
    public bool IsActive { get; set; }
    public bool HasPermission { get; set; }
    public bool CanEdit { get; set; }
}

public class BooleanPropertyProfile : Profile
{
    public BooleanPropertyProfile()
    {
        CreateMap<SourceWithBooleans, DestWithBooleans>();
    }
}

// Numeric Properties
public class SourceWithNumbers
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public string Item3Name { get; set; } = string.Empty;
}

public class DestWithNumbers
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public string Item3Name { get; set; } = string.Empty;
}

public class NumericPropertyProfile : Profile
{
    public NumericPropertyProfile()
    {
        CreateMap<SourceWithNumbers, DestWithNumbers>();
    }
}

#endregion
