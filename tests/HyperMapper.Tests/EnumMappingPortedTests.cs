using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 Enumerations.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class EnumMappingPortedTests
{
    #region Shared Enum Mapping Tests

    [Fact]
    public void ShouldMapSharedEnum()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SharedEnumProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrder { Status = EnumStatus.InProgress };
        var dto = mapper.Map<EnumOrderDto>(order);

        Assert.Equal(EnumStatus.InProgress, dto.Status);
    }

    [Fact]
    public void ShouldMapToUnderlyingType()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumToIntProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrder { Status = EnumStatus.InProgress };
        var dto = mapper.Map<EnumOrderDtoInt>(order);

        Assert.Equal(1, dto.Status);
    }

    [Fact]
    public void ShouldMapToStringType()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumToStringProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrder { Status = EnumStatus.InProgress };
        var dto = mapper.Map<EnumOrderDtoString>(order);

        Assert.Equal("InProgress", dto.Status);
    }

    [Fact]
    public void ShouldMapFromUnderlyingType()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IntToEnumProfile>());
        var mapper = config.CreateMapper();

        var dto = new EnumOrderDtoInt { Status = 1 };
        var order = mapper.Map<EnumOrder>(dto);

        Assert.Equal(EnumStatus.InProgress, order.Status);
    }

    [Fact]
    public void ShouldMapFromStringType()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringToEnumProfile>());
        var mapper = config.CreateMapper();

        var dto = new EnumOrderDtoString { Status = "InProgress" };
        var order = mapper.Map<EnumOrder>(dto);

        Assert.Equal(EnumStatus.InProgress, order.Status);
    }

    #endregion

    #region Enum By Matching Names Tests

    [Fact]
    public void ShouldMapEnumByMatchingNames()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumByNameProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrder { Status = EnumStatus.InProgress };
        var dto = mapper.Map<EnumOrderDtoWithOwnStatus>(order);

        Assert.Equal(StatusForDto.InProgress, dto.Status);
    }

    #endregion

    #region Nullable Enum Tests

    [Fact]
    public void ShouldMapSharedNullableEnum()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableEnumProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrderWithNullableStatus { Status = EnumStatus.InProgress };
        var dto = mapper.Map<EnumOrderDtoWithNullableStatus>(order);

        Assert.Equal(EnumStatus.InProgress, dto.Status);
    }

    [Fact]
    public void ShouldMapNullableEnumByMatchingValues()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableEnumMatchingProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrderWithNullableStatus { Status = EnumStatus.InProgress };
        var dto = mapper.Map<EnumOrderDtoWithOwnNullableStatus>(order);

        Assert.Equal(StatusForDto.InProgress, dto.Status);
    }

    [Fact]
    public void ShouldMapNullableEnumToNullWhenSourceEnumIsNull()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableEnumMatchingProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrderWithNullableStatus { Status = null };
        var dto = mapper.Map<EnumOrderDtoWithOwnNullableStatus>(order);

        Assert.Null(dto.Status);
    }

    #endregion

    #region Enum to Nullable Enum Tests

    [Fact]
    public void ShouldMapEnumToNullableEnum()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumToNullableProfile>());
        var mapper = config.CreateMapper();

        var source = new EnumSource { Values = EnumValues.Two | EnumValues.Three };
        var dest = mapper.Map<EnumNullableDest>(source);

        Assert.Equal(EnumValues.Two | EnumValues.Three, dest.Values);
    }

    #endregion

    #region Default Enum Value Tests

    [Fact]
    public void DefaultEnumValueToString_Should_map_ok()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DefaultEnumToStringProfile>());
        var mapper = config.CreateMapper();

        var source = new ColorSource { Color = default };
        var dest = mapper.Map<ColorStringDest>(source);

        Assert.Equal("Black", dest.Color);
    }

    #endregion

    #region String To Nullable Enum Tests

    [Fact]
    public void StringToNullableEnum_Should_map_with_underlying_type()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringToNullableEnumProfile>());
        var mapper = config.CreateMapper();

        var source = new StringColorSource { Color = "Red" };
        var dest = mapper.Map<NullableColorDest>(source);

        Assert.Equal(ConsoleColor.Red, dest.Color);
    }

    #endregion

    #region Nullable Enum To String Tests

    [Fact]
    public void NullableEnumToString_Should_map_value()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableEnumToStringProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableColorSource { Color = ConsoleColor.Blue };
        var dest = mapper.Map<ColorStringDest>(source);

        Assert.Equal("Blue", dest.Color);
    }

    [Fact]
    public void NullableEnumToString_Should_map_null()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableEnumToStringProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableColorSource { Color = null };
        var dest = mapper.Map<ColorStringDest>(source);

        Assert.Null(dest.Color);
    }

    #endregion

    #region Flags Enum Tests

    [Fact]
    public void When_mapping_a_flags_enum_Should_include_all_source_enum_values()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FlagsEnumProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceFlags { Flags = SourceFlagsEnum.One | SourceFlagsEnum.Four | SourceFlagsEnum.Eight };
        var dest = mapper.Map<DestFlags>(source);

        Assert.Equal(DestFlagsEnum.One | DestFlagsEnum.Four | DestFlagsEnum.Eight, dest.Flags);
    }

    #endregion

    #region Enum With Invalid Value Tests

    [Fact]
    public void ShouldMapEnumWithInvalidValue()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumByNameProfile>());
        var mapper = config.CreateMapper();

        var order = new EnumOrder { Status = 0 };
        var dto = mapper.Map<EnumOrderDtoWithOwnStatus>(order);

        var expected = (StatusForDto)0;
        Assert.Equal(expected, dto.Status);
    }

    #endregion
}

#region Test Classes and Profiles

// Shared Enum
public enum EnumStatus
{
    InProgress = 1,
    Complete = 2
}

public enum StatusForDto
{
    InProgress = 1,
    Complete = 2
}

public class EnumOrder
{
    public EnumStatus Status { get; set; }
}

public class EnumOrderDto
{
    public EnumStatus Status { get; set; }
}

public class EnumOrderDtoInt
{
    public int Status { get; set; }
}

public class EnumOrderDtoString
{
    public string Status { get; set; } = string.Empty;
}

public class EnumOrderDtoWithOwnStatus
{
    public StatusForDto Status { get; set; }
}

public class EnumOrderWithNullableStatus
{
    public EnumStatus? Status { get; set; }
}

public class EnumOrderDtoWithNullableStatus
{
    public EnumStatus? Status { get; set; }
}

public class EnumOrderDtoWithOwnNullableStatus
{
    public StatusForDto? Status { get; set; }
}

// Profiles
public class SharedEnumProfile : Profile
{
    public SharedEnumProfile()
    {
        CreateMap<EnumOrder, EnumOrderDto>();
    }
}

public class EnumToIntProfile : Profile
{
    public EnumToIntProfile()
    {
        CreateMap<EnumOrder, EnumOrderDtoInt>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => (int)s.Status));
    }
}

public class EnumToStringProfile : Profile
{
    public EnumToStringProfile()
    {
        CreateMap<EnumOrder, EnumOrderDtoString>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}

public class IntToEnumProfile : Profile
{
    public IntToEnumProfile()
    {
        CreateMap<EnumOrderDtoInt, EnumOrder>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => (EnumStatus)s.Status));
    }
}

public class StringToEnumProfile : Profile
{
    public StringToEnumProfile()
    {
        CreateMap<EnumOrderDtoString, EnumOrder>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => Enum.Parse<EnumStatus>(s.Status)));
    }
}

public class EnumByNameProfile : Profile
{
    public EnumByNameProfile()
    {
        CreateMap<EnumOrder, EnumOrderDtoWithOwnStatus>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => (StatusForDto)(int)s.Status));
    }
}

public class NullableEnumProfile : Profile
{
    public NullableEnumProfile()
    {
        CreateMap<EnumOrderWithNullableStatus, EnumOrderDtoWithNullableStatus>();
    }
}

public class NullableEnumMatchingProfile : Profile
{
    public NullableEnumMatchingProfile()
    {
        CreateMap<EnumOrderWithNullableStatus, EnumOrderDtoWithOwnNullableStatus>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.HasValue ? (StatusForDto?)(int)s.Status.Value : null));
    }
}

// Flags Enum
[Flags]
public enum EnumValues
{
    One, Two = 2, Three = 4
}

public class EnumSource
{
    public EnumValues Values { get; set; }
}

public class EnumNullableDest
{
    public EnumValues? Values { get; set; }
}

public class EnumToNullableProfile : Profile
{
    public EnumToNullableProfile()
    {
        CreateMap<EnumSource, EnumNullableDest>();
    }
}

// Color Enum
public class ColorSource
{
    public ConsoleColor Color { get; set; }
}

public class ColorStringDest
{
    public string? Color { get; set; }
}

public class DefaultEnumToStringProfile : Profile
{
    public DefaultEnumToStringProfile()
    {
        CreateMap<ColorSource, ColorStringDest>()
            .ForMember(d => d.Color, opt => opt.MapFrom(s => s.Color.ToString()));
    }
}

public class StringColorSource
{
    public string Color { get; set; } = string.Empty;
}

public class NullableColorDest
{
    public ConsoleColor? Color { get; set; }
}

public class StringToNullableEnumProfile : Profile
{
    public StringToNullableEnumProfile()
    {
        CreateMap<StringColorSource, NullableColorDest>()
            .ForMember(d => d.Color, opt => opt.MapFrom(s => Enum.Parse<ConsoleColor>(s.Color)));
    }
}

public class NullableColorSource
{
    public ConsoleColor? Color { get; set; }
}

public class NullableEnumToStringProfile : Profile
{
    public NullableEnumToStringProfile()
    {
        CreateMap<NullableColorSource, ColorStringDest>()
            .ForMember(d => d.Color, opt => opt.MapFrom(s => s.Color.HasValue ? s.Color.Value.ToString() : null));
    }
}

// Flags Enum
[Flags]
public enum SourceFlagsEnum
{
    None = 0,
    One = 1,
    Two = 2,
    Four = 4,
    Eight = 8
}

[Flags]
public enum DestFlagsEnum
{
    None = 0,
    One = 1,
    Two = 2,
    Four = 4,
    Eight = 8
}

public class SourceFlags
{
    public SourceFlagsEnum Flags { get; set; }
}

public class DestFlags
{
    public DestFlagsEnum Flags { get; set; }
}

public class FlagsEnumProfile : Profile
{
    public FlagsEnumProfile()
    {
        CreateMap<SourceFlags, DestFlags>()
            .ForMember(d => d.Flags, opt => opt.MapFrom(s => (DestFlagsEnum)(int)s.Flags));
    }
}

#endregion
