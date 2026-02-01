using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for implicit/explicit conversion operators ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests/Mappers
/// License: MIT
///
/// Note: HyperMapper relies on convention-based mapping.
/// These tests verify mapping with types that have conversion operators.
/// </summary>
public class ConversionOperatorsPortedTests
{
    #region Implicit Conversion Tests

    [Fact]
    public void Should_map_type_with_implicit_conversion_to_string()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ImplicitConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithConvCustomId
        {
            Id = new ConvCustomId(123),
            Name = "Test"
        };

        var dest = mapper.Map<DestWithStringId>(source);

        Assert.Equal("123", dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Should_map_string_to_type_with_implicit_conversion()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StringToCustomProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringId
        {
            Id = "456",
            Name = "Test"
        };

        var dest = mapper.Map<DestWithConvCustomId>(source);

        Assert.Equal(456, dest.Id.Value);
        Assert.Equal("Test", dest.Name);
    }

    #endregion

    #region Explicit Conversion Tests

    [Fact]
    public void Should_map_with_explicit_conversion_via_ForMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ExplicitConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithMoney
        {
            Amount = new Money(100.50m),
            Description = "Payment"
        };

        var dest = mapper.Map<ConvDestWithDecimal>(source);

        Assert.Equal(100.50m, dest.Amount);
        Assert.Equal("Payment", dest.Description);
    }

    #endregion

    #region Complex Type with Operators Tests

    [Fact]
    public void Should_map_complex_type_with_nested_conversion()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new ConvOrderSource
        {
            OrderId = new OrderId(1001),
            Customer = new CustomerSource { Name = "John" }
        };

        var dest = mapper.Map<ConvOrderDest>(source);

        Assert.Equal("1001", dest.OrderId);
        Assert.NotNull(dest.Customer);
        Assert.Equal("John", dest.Customer.Name);
    }

    [Fact]
    public void Should_map_collection_of_types_with_conversion()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollectionConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithIdList
        {
            Ids = new List<ConvCustomId>
            {
                new(1),
                new(2),
                new(3)
            }
        };

        var dest = mapper.Map<DestWithStringList>(source);

        Assert.Equal(3, dest.Ids.Count);
        Assert.Contains("1", dest.Ids);
        Assert.Contains("2", dest.Ids);
        Assert.Contains("3", dest.Ids);
    }

    #endregion

    #region Nullable with Conversion Tests

    [Fact]
    public void Should_handle_nullable_with_conversion()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NullableConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableId
        {
            Id = new ConvCustomId(789),
            OptionalId = null
        };

        var dest = mapper.Map<DestWithNullableString>(source);

        Assert.Equal("789", dest.Id);
        Assert.Null(dest.OptionalId);
    }

    #endregion
}

#region Test Classes with Conversion Operators

// ConvCustomId with implicit conversions
public readonly struct ConvCustomId
{
    public int Value { get; }

    public ConvCustomId(int value) => Value = value;

    public static implicit operator string(ConvCustomId id) => id.Value.ToString();
    public static implicit operator ConvCustomId(string s) => new(int.TryParse(s, out var v) ? v : 0);
    public static implicit operator int(ConvCustomId id) => id.Value;
    public static implicit operator ConvCustomId(int v) => new(v);
}

// Money with explicit conversion
public readonly struct Money
{
    public decimal Amount { get; }

    public Money(decimal amount) => Amount = amount;

    public static explicit operator decimal(Money m) => m.Amount;
    public static explicit operator Money(decimal d) => new(d);
}

// OrderId
public readonly struct OrderId
{
    public int Value { get; }

    public OrderId(int value) => Value = value;

    public static implicit operator string(OrderId id) => id.Value.ToString();
}

#endregion

#region Test Classes and Profiles

// Implicit Conversion Source/Dest
public class SourceWithConvCustomId
{
    public ConvCustomId Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DestWithStringId
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ImplicitConversionProfile : Profile
{
    public ImplicitConversionProfile()
    {
        CreateMap<SourceWithConvCustomId, DestWithStringId>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => (string)s.Id));
    }
}

// String to Custom
public class SourceWithStringId
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class DestWithConvCustomId
{
    public ConvCustomId Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StringToCustomProfile : Profile
{
    public StringToCustomProfile()
    {
        CreateMap<SourceWithStringId, DestWithConvCustomId>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => (ConvCustomId)s.Id));
    }
}

// Explicit Conversion
public class SourceWithMoney
{
    public Money Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ConvDestWithDecimal
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ExplicitConversionProfile : Profile
{
    public ExplicitConversionProfile()
    {
        CreateMap<SourceWithMoney, ConvDestWithDecimal>()
            .ForMember(d => d.Amount, opt => opt.MapFrom(s => (decimal)s.Amount));
    }
}

// Nested Conversion
public class CustomerSource
{
    public string Name { get; set; } = string.Empty;
}

public class CustomerDest
{
    public string Name { get; set; } = string.Empty;
}

public class ConvOrderSource
{
    public OrderId OrderId { get; set; }
    public CustomerSource? Customer { get; set; }
}

public class ConvOrderDest
{
    public string OrderId { get; set; } = string.Empty;
    public CustomerDest? Customer { get; set; }
}

public class NestedConversionProfile : Profile
{
    public NestedConversionProfile()
    {
        CreateMap<CustomerSource, CustomerDest>();
        CreateMap<ConvOrderSource, ConvOrderDest>()
            .ForMember(d => d.OrderId, opt => opt.MapFrom(s => (string)s.OrderId));
    }
}

// Collection Conversion
public class SourceWithIdList
{
    public List<ConvCustomId> Ids { get; set; } = new();
}

public class DestWithStringList
{
    public List<string> Ids { get; set; } = new();
}

public class CollectionConversionProfile : Profile
{
    public CollectionConversionProfile()
    {
        CreateMap<SourceWithIdList, DestWithStringList>()
            .ForMember(d => d.Ids, opt => opt.MapFrom(s => s.Ids.Select(id => (string)id).ToList()));
    }
}

// Nullable Conversion
public class SourceWithNullableId
{
    public ConvCustomId Id { get; set; }
    public ConvCustomId? OptionalId { get; set; }
}

public class DestWithNullableString
{
    public string Id { get; set; } = string.Empty;
    public string? OptionalId { get; set; }
}

public class NullableConversionProfile : Profile
{
    public NullableConversionProfile()
    {
        CreateMap<SourceWithNullableId, DestWithNullableString>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => (string)s.Id))
            .ForMember(d => d.OptionalId, opt => opt.MapFrom(s => s.OptionalId.HasValue ? (string)s.OptionalId.Value : null));
    }
}

#endregion
