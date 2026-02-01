using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Advanced tests for PreCondition functionality ported from AutoMapper v14.0.0
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class PreConditionAdvancedPortedTests
{
    #region Basic PreCondition Tests

    [Fact]
    public void PreCondition_should_skip_mapping_when_false()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondBasicProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondSource { Value = 0, ShouldMap = false };
        var dest = new PreCondDest { Value = 999 };

        mapper.Map(source, dest);

        Assert.Equal(999, dest.Value); // Should not be changed
    }

    [Fact]
    public void PreCondition_should_map_when_true()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondBasicProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondSource { Value = 42, ShouldMap = true };
        var dest = new PreCondDest { Value = 999 };

        mapper.Map(source, dest);

        Assert.Equal(42, dest.Value);
    }

    #endregion

    #region PreCondition with Null Checks

    [Fact]
    public void PreCondition_should_handle_null_source_value()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondNullCheckProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondNullSource { Name = null };
        var dest = new PreCondNullDest { Name = "original" };

        mapper.Map(source, dest);

        Assert.Equal("original", dest.Name); // Should not change because Name is null
    }

    [Fact]
    public void PreCondition_should_map_when_not_null()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondNullCheckProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondNullSource { Name = "new value" };
        var dest = new PreCondNullDest { Name = "original" };

        mapper.Map(source, dest);

        Assert.Equal("new value", dest.Name);
    }

    #endregion

    #region PreCondition Based on Source Property Value

    [Fact]
    public void PreCondition_based_on_other_property_value()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondPropertyValueProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondPropertySource { Amount = 100, IsActive = false };
        var dest = new PreCondPropertyDest { Amount = 0 };

        mapper.Map(source, dest);

        Assert.Equal(0, dest.Amount); // Should not map because IsActive is false
    }

    [Fact]
    public void PreCondition_maps_when_property_condition_met()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondPropertyValueProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondPropertySource { Amount = 100, IsActive = true };
        var dest = new PreCondPropertyDest { Amount = 0 };

        mapper.Map(source, dest);

        Assert.Equal(100, dest.Amount);
    }

    #endregion

    #region Multiple PreConditions

    [Fact]
    public void Multiple_properties_with_different_preconditions()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultiplePreCondProfile>());
        var mapper = config.CreateMapper();

        var source = new MultiPreCondSource
        {
            Value1 = 10,
            Value2 = 20,
            MapValue1 = true,
            MapValue2 = false
        };
        var dest = new MultiPreCondDest { Value1 = 0, Value2 = 0 };

        mapper.Map(source, dest);

        Assert.Equal(10, dest.Value1); // Should be mapped
        Assert.Equal(0, dest.Value2);  // Should not be mapped
    }

    #endregion

    #region PreCondition with Complex Logic

    [Fact]
    public void PreCondition_with_range_check()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondRangeProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondRangeSource { Score = 150 }; // Outside valid range 0-100
        var dest = new PreCondRangeDest { Score = 50 };

        mapper.Map(source, dest);

        Assert.Equal(50, dest.Score); // Should not change because outside range
    }

    [Fact]
    public void PreCondition_maps_when_in_range()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondRangeProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondRangeSource { Score = 85 }; // Within valid range 0-100
        var dest = new PreCondRangeDest { Score = 50 };

        mapper.Map(source, dest);

        Assert.Equal(85, dest.Score);
    }

    #endregion

    #region PreCondition with MapFrom

    [Fact]
    public void PreCondition_with_custom_mapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondMapFromProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondMapFromSource { FirstName = "John", LastName = "Doe", HasFullName = true };
        var dest = new PreCondMapFromDest { FullName = "" };

        mapper.Map(source, dest);

        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void PreCondition_skips_custom_mapping_when_false()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<PreCondMapFromProfile>());
        var mapper = config.CreateMapper();

        var source = new PreCondMapFromSource { FirstName = "John", LastName = "Doe", HasFullName = false };
        var dest = new PreCondMapFromDest { FullName = "Original" };

        mapper.Map(source, dest);

        Assert.Equal("Original", dest.FullName);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic PreCondition
public class PreCondSource
{
    public int Value { get; set; }
    public bool ShouldMap { get; set; }
}

public class PreCondDest
{
    public int Value { get; set; }
}

public class PreCondBasicProfile : Profile
{
    public PreCondBasicProfile()
    {
        CreateMap<PreCondSource, PreCondDest>()
            .ForMember(d => d.Value, opt =>
            {
                opt.PreCondition(src => src.ShouldMap);
                opt.MapFrom(src => src.Value);
            });
    }
}

// PreCondition Null Check
public class PreCondNullSource
{
    public string? Name { get; set; }
}

public class PreCondNullDest
{
    public string? Name { get; set; }
}

public class PreCondNullCheckProfile : Profile
{
    public PreCondNullCheckProfile()
    {
        CreateMap<PreCondNullSource, PreCondNullDest>()
            .ForMember(d => d.Name, opt =>
            {
                opt.PreCondition(src => src.Name != null);
                opt.MapFrom(src => src.Name);
            });
    }
}

// PreCondition Property Value
public class PreCondPropertySource
{
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
}

public class PreCondPropertyDest
{
    public decimal Amount { get; set; }
}

public class PreCondPropertyValueProfile : Profile
{
    public PreCondPropertyValueProfile()
    {
        CreateMap<PreCondPropertySource, PreCondPropertyDest>()
            .ForMember(d => d.Amount, opt =>
            {
                opt.PreCondition(src => src.IsActive);
                opt.MapFrom(src => src.Amount);
            });
    }
}

// Multiple PreConditions
public class MultiPreCondSource
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public bool MapValue1 { get; set; }
    public bool MapValue2 { get; set; }
}

public class MultiPreCondDest
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
}

public class MultiplePreCondProfile : Profile
{
    public MultiplePreCondProfile()
    {
        CreateMap<MultiPreCondSource, MultiPreCondDest>()
            .ForMember(d => d.Value1, opt =>
            {
                opt.PreCondition(src => src.MapValue1);
                opt.MapFrom(src => src.Value1);
            })
            .ForMember(d => d.Value2, opt =>
            {
                opt.PreCondition(src => src.MapValue2);
                opt.MapFrom(src => src.Value2);
            });
    }
}

// PreCondition Range
public class PreCondRangeSource
{
    public int Score { get; set; }
}

public class PreCondRangeDest
{
    public int Score { get; set; }
}

public class PreCondRangeProfile : Profile
{
    public PreCondRangeProfile()
    {
        CreateMap<PreCondRangeSource, PreCondRangeDest>()
            .ForMember(d => d.Score, opt =>
            {
                opt.PreCondition(src => src.Score >= 0 && src.Score <= 100);
                opt.MapFrom(src => src.Score);
            });
    }
}

// PreCondition MapFrom
public class PreCondMapFromSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool HasFullName { get; set; }
}

public class PreCondMapFromDest
{
    public string FullName { get; set; } = string.Empty;
}

public class PreCondMapFromProfile : Profile
{
    public PreCondMapFromProfile()
    {
        CreateMap<PreCondMapFromSource, PreCondMapFromDest>()
            .ForMember(d => d.FullName, opt =>
            {
                opt.PreCondition(src => src.HasFullName);
                opt.MapFrom(src => $"{src.FirstName} {src.LastName}");
            });
    }
}

#endregion
