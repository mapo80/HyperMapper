using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 TypeConverters.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class TypeConverterPortedTests
{
    #region String to Enum Converter Tests

    [Fact]
    public void StringToEnumConverter_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PortedStringToEnumProfile>();
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<StringEnumDest>(new StringEnumSource { Enum = "DarkCyan" });
        Assert.Equal(ConsoleColor.DarkCyan, result.Enum);
    }

    #endregion

    #region Nullable Converter Tests

    [Fact]
    public void NullableConverter_Should_map_nullable_with_value()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PortedNullableConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new NullableIntSource { Value = 42 };
        var dest = mapper.Map<GreekLettersDest>(source);
        Assert.Equal(GreekLetters.Gamma, dest.Letter);
    }

    [Fact]
    public void NullableConverter_Should_map_nullable_null()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PortedNullableConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new NullableIntSource { Value = null };
        var dest = mapper.Map<GreekLettersDest>(source);
        Assert.Equal(GreekLetters.Beta, dest.Letter);
    }

    #endregion

    #region Decimal and Nullable Decimal Tests

    [Fact]
    public void DecimalAndNullableDecimal_Should_treat_max_value_as_null()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DecimalNullableProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new DecimalSource { Value1 = decimal.MaxValue, Value2 = null, Value3 = null };
        var dest = mapper.Map<DecimalDest>(source);

        Assert.Null(dest.Value1);
        Assert.Equal(decimal.MaxValue, dest.Value2);
        Assert.Null(dest.Value3);
    }

    #endregion

    #region Converting to String Tests

    [Fact]
    public void When_converting_to_string_Should_use_the_type_converter()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<IdToStringProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new IdSource { TheId = new CustomId { Prefix = "p", Value = "v" } };
        var dest = mapper.Map<IdStringDest>(source);

        Assert.Equal("p_v", dest.TheId);
    }

    #endregion

    #region Specifying Type Converters Tests

    [Fact]
    public void When_specifying_type_converters_Should_convert_type_using_expression()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PortedMultiConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ConverterMultiSource { Value1 = "5", Value2 = "01/01/2000" };
        var dest = mapper.Map<ConverterMultiDest>(source);

        Assert.Equal(5, dest.Value1);
    }

    [Fact]
    public void When_specifying_type_converters_Should_convert_type_using_instance()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PortedMultiConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ConverterMultiSource { Value1 = "5", Value2 = "01/01/2000" };
        var dest = mapper.Map<ConverterMultiDest>(source);

        Assert.Equal(new DateTime(2000, 1, 1), dest.Value2);
    }

    #endregion

    #region Type Converter with Incompatible Members Tests

    [Fact]
    public void When_specifying_type_converters_on_types_with_incompatible_members_Should_use_converter()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<IncompatibleMemberProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ParentIncompatibleSource { Value = new IncompatibleSource { Foo = "5" } };
        var dest = mapper.Map<ParentIncompatibleDest>(source);

        Assert.Equal(5, dest.Value.Type);
    }

    #endregion

    #region Type Converter for Non-Generic Configuration Tests

    [Fact]
    public void When_specifying_a_type_converter_for_a_non_generic_configuration_Should_use_converter_specified()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NonGenericConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var dest = mapper.Map<NonGenericDest>(new NonGenericSource { Value = 5 });

        Assert.Equal(15, dest.OtherValue);
    }

    #endregion

    #region Lambda Converter Tests

    [Fact]
    public void ConvertUsing_WithLambda_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<LambdaConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new PortedSimpleSource { Value = 10 };
        var dest = mapper.Map<PortedSimpleStringDest>(source);

        Assert.Equal("Value: 10", dest.Value);
    }

    #endregion

    #region Converter with Context Access Tests

    [Fact]
    public void ConvertUsing_WithContextMapperAccess_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ContextAccessConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new PortedWrapperSource { Inner = new PortedInnerSource { Id = 42 } };
        var dest = mapper.Map<PortedWrapperDest>(source);

        Assert.NotNull(dest.Inner);
        Assert.Equal(42, dest.Inner.Id);
    }

    #endregion
}

#region Test Classes and Profiles

// String to Enum
public class StringEnumSource
{
    public string Enum { get; set; } = string.Empty;
}

public class StringEnumDest
{
    public ConsoleColor Enum { get; set; }
}

public class PortedStringToEnumProfile : Profile
{
    public PortedStringToEnumProfile()
    {
        CreateMap<StringEnumSource, StringEnumDest>();
    }
}

// Nullable Converter
public enum GreekLetters
{
    Alpha = 11,
    Beta = 12,
    Gamma = 13
}

public class NullableIntSource
{
    public int? Value { get; set; }
}

public class GreekLettersDest
{
    public GreekLetters Letter { get; set; }
}

public class PortedNullableConverterProfile : Profile
{
    public PortedNullableConverterProfile()
    {
        CreateMap<NullableIntSource, GreekLettersDest>()
            .ConvertUsing(new NullableToGreekLetterConverter());
    }
}

public class NullableToGreekLetterConverter : ITypeConverter<NullableIntSource, GreekLettersDest>
{
    public GreekLettersDest Convert(NullableIntSource source, GreekLettersDest destination, ResolutionContext context)
    {
        return new GreekLettersDest
        {
            Letter = source.Value == null ? GreekLetters.Beta : GreekLetters.Gamma
        };
    }
}

// Decimal and Nullable Decimal
public class DecimalSource
{
    public decimal Value1 { get; set; }
    public decimal? Value2 { get; set; }
    public decimal? Value3 { get; set; }
}

public class DecimalDest
{
    public decimal? Value1 { get; set; }
    public decimal Value2 { get; set; }
    public decimal? Value3 { get; set; }
}

public class DecimalNullableProfile : Profile
{
    public DecimalNullableProfile()
    {
        CreateMap<DecimalSource, DecimalDest>()
            .ConvertUsing(new DecimalNullableConverter());
    }
}

public class DecimalNullableConverter : ITypeConverter<DecimalSource, DecimalDest>
{
    public DecimalDest Convert(DecimalSource source, DecimalDest destination, ResolutionContext context)
    {
        return new DecimalDest
        {
            Value1 = source.Value1 == decimal.MaxValue ? null : source.Value1,
            Value2 = source.Value2 ?? decimal.MaxValue,
            Value3 = source.Value3
        };
    }
}

// Id to String
public interface ICustomId
{
    string Serialize();
}

public class CustomId : ICustomId
{
    public string Prefix { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public string Serialize() => Prefix + "_" + Value;
}

public class IdSource
{
    public CustomId TheId { get; set; } = new();
}

public class IdStringDest
{
    public string TheId { get; set; } = string.Empty;
}

public class IdToStringProfile : Profile
{
    public IdToStringProfile()
    {
        CreateMap<IdSource, IdStringDest>()
            .ForMember(d => d.TheId, opt => opt.MapFrom(s => s.TheId.Serialize()));
    }
}

// Multi Converter
public class ConverterMultiSource
{
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
}

public class ConverterMultiDest
{
    public int Value1 { get; set; }
    public DateTime Value2 { get; set; }
}

public class PortedMultiConverterProfile : Profile
{
    public PortedMultiConverterProfile()
    {
        CreateMap<ConverterMultiSource, ConverterMultiDest>()
            .ForMember(d => d.Value1, opt => opt.MapFrom(s => int.Parse(s.Value1)))
            .ForMember(d => d.Value2, opt => opt.MapFrom(s => DateTime.Parse(s.Value2)));
    }
}

// Incompatible Members
public class IncompatibleSource
{
    public string Foo { get; set; } = string.Empty;
}

public class IncompatibleDest
{
    public int Type { get; set; }
}

public class ParentIncompatibleSource
{
    public IncompatibleSource Value { get; set; } = new();
}

public class ParentIncompatibleDest
{
    public IncompatibleDest Value { get; set; } = new();
}

public class IncompatibleMemberProfile : Profile
{
    public IncompatibleMemberProfile()
    {
        CreateMap<IncompatibleSource, IncompatibleDest>()
            .ConvertUsing(new IncompatibleConverter());
        CreateMap<ParentIncompatibleSource, ParentIncompatibleDest>();
    }
}

public class IncompatibleConverter : ITypeConverter<IncompatibleSource, IncompatibleDest>
{
    public IncompatibleDest Convert(IncompatibleSource source, IncompatibleDest destination, ResolutionContext context)
    {
        return new IncompatibleDest { Type = int.Parse(source.Foo) };
    }
}

// Non-Generic Converter
public class NonGenericSource
{
    public int Value { get; set; }
}

public class NonGenericDest
{
    public int OtherValue { get; set; }
}

public class NonGenericConverterProfile : Profile
{
    public NonGenericConverterProfile()
    {
        CreateMap<NonGenericSource, NonGenericDest>()
            .ConvertUsing<NonGenericCustomConverter>();
    }
}

public class NonGenericCustomConverter : ITypeConverter<NonGenericSource, NonGenericDest>
{
    public NonGenericDest Convert(NonGenericSource source, NonGenericDest destination, ResolutionContext context)
    {
        return new NonGenericDest { OtherValue = source.Value + 10 };
    }
}

// Lambda Converter
public class PortedSimpleSource
{
    public int Value { get; set; }
}

public class PortedSimpleStringDest
{
    public string Value { get; set; } = string.Empty;
}

public class LambdaConverterProfile : Profile
{
    public LambdaConverterProfile()
    {
        CreateMap<PortedSimpleSource, PortedSimpleStringDest>()
            .ForMember(d => d.Value, opt => opt.MapFrom(s => $"Value: {s.Value}"));
    }
}

// Context Access Converter
public class PortedInnerSource
{
    public int Id { get; set; }
}

public class PortedInnerDest
{
    public int Id { get; set; }
}

public class PortedWrapperSource
{
    public PortedInnerSource? Inner { get; set; }
}

public class PortedWrapperDest
{
    public PortedInnerDest? Inner { get; set; }
}

public class ContextAccessConverterProfile : Profile
{
    public ContextAccessConverterProfile()
    {
        CreateMap<PortedInnerSource, PortedInnerDest>();
        CreateMap<PortedWrapperSource, PortedWrapperDest>()
            .ConvertUsing(new PortedWrapperConverter());
    }
}

public class PortedWrapperConverter : ITypeConverter<PortedWrapperSource, PortedWrapperDest>
{
    public PortedWrapperDest Convert(PortedWrapperSource source, PortedWrapperDest destination, ResolutionContext context)
    {
        return new PortedWrapperDest
        {
            Inner = context.Mapper.Map<PortedInnerDest>(source.Inner!)
        };
    }
}

#endregion
