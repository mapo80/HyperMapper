using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 NullSubstitute() feature - null value substitution.
/// AutoMapper API compatibility: opt.NullSubstitute("N/A")
/// </summary>
public class NullSubstituteTests
{
    #region Test Models

    public class NullSubSource
    {
        public string? Name { get; set; }
        public string? Title { get; set; }
        public int? NullableInt { get; set; }
        public decimal? NullableDecimal { get; set; }
        public DateTime? NullableDate { get; set; }
        public InnerSource? Inner { get; set; }
    }

    public class InnerSource
    {
        public string? Data { get; set; }
    }

    public class NullSubDestination
    {
        public string? Name { get; set; }
        public string? Title { get; set; }
        public int? NullableInt { get; set; }
        public decimal? NullableDecimal { get; set; }
        public DateTime? NullableDate { get; set; }
        public string? InnerData { get; set; }
    }

    #endregion

    #region Profiles

    public class NullSubstituteBasicProfile : Profile
    {
        public NullSubstituteBasicProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("N/A");
                });
        }
    }

    public class NullSubstituteEmptyProfile : Profile
    {
        public NullSubstituteEmptyProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("(empty)");
                });
        }
    }

    public class NullSubstituteIntProfile : Profile
    {
        public NullSubstituteIntProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.NullableInt, opt =>
                {
                    opt.MapFrom(s => s.NullableInt);
                    opt.NullSubstitute(-1);
                });
        }
    }

    public class NullSubstituteDecimalProfile : Profile
    {
        public NullSubstituteDecimalProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.NullableDecimal, opt =>
                {
                    opt.MapFrom(s => s.NullableDecimal);
                    opt.NullSubstitute(0.0m);
                });
        }
    }

    public class NullSubstituteDateProfile : Profile
    {
        public static DateTime DefaultDate = new DateTime(2000, 1, 1);

        public NullSubstituteDateProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.NullableDate, opt =>
                {
                    opt.MapFrom(s => s.NullableDate);
                    opt.NullSubstitute(DefaultDate);
                });
        }
    }

    public class NullSubstituteMultipleProfile : Profile
    {
        public NullSubstituteMultipleProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("No Name");
                })
                .ForMember(d => d.Title, opt =>
                {
                    opt.MapFrom(s => s.Title);
                    opt.NullSubstitute("No Title");
                });
        }
    }

    public class NullSubstituteWithMapFromProfile : Profile
    {
        public NullSubstituteWithMapFromProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.InnerData, opt =>
                {
                    opt.MapFrom(s => s.Inner != null ? s.Inner.Data : null);
                    opt.NullSubstitute("No Data");
                });
        }
    }

    public class NullSubstituteWithConditionProfile : Profile
    {
        public NullSubstituteWithConditionProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("N/A");
                    // This condition is evaluated AFTER NullSubstitute is applied
                    opt.Condition((src, dest, srcMember) => srcMember != null && srcMember.Length > 1);
                });
        }
    }

    public class NullSubstituteWithPreConditionProfile : Profile
    {
        public NullSubstituteWithPreConditionProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.PreCondition(s => s.NullableInt.HasValue);  // This will fail
                    opt.NullSubstitute("N/A");
                });
        }
    }

    public class NullSubstituteAutoConventionProfile : Profile
    {
        public NullSubstituteAutoConventionProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt => opt.NullSubstitute("Default"));
        }
    }

    public class NullSubstituteInt999Profile : Profile
    {
        public NullSubstituteInt999Profile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.NullableInt, opt =>
                {
                    opt.MapFrom(s => s.NullableInt);
                    opt.NullSubstitute(-999);
                });
        }
    }

    public class NullSubstituteSubstitutedProfile : Profile
    {
        public NullSubstituteSubstitutedProfile()
        {
            CreateMap<NullSubSource, NullSubDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("Substituted");
                });
        }
    }

    #endregion

    [Fact]
    public void NullSubstitute_WhenSourceNull_ReturnsSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteBasicProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = null };

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert
        Assert.Equal("N/A", result.Name);
    }

    [Fact]
    public void NullSubstitute_WhenSourceNotNull_ReturnsOriginal()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteBasicProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = "John" };

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert
        Assert.Equal("John", result.Name);
    }

    [Fact]
    public void NullSubstitute_WithValueType_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteIntProfile>());
        var mapper = config.CreateMapper();

        // Act - Null source
        var source1 = new NullSubSource { NullableInt = null };
        var result1 = mapper.Map<NullSubDestination>(source1);
        Assert.Equal(-1, result1.NullableInt);

        // Act - Has value
        var source2 = new NullSubSource { NullableInt = 42 };
        var result2 = mapper.Map<NullSubDestination>(source2);
        Assert.Equal(42, result2.NullableInt);
    }

    [Fact]
    public void NullSubstitute_WithReferenceType_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteEmptyProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = null };

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert
        Assert.Equal("(empty)", result.Name);
    }

    [Fact]
    public void NullSubstitute_WithEmptyString_TreatsAsNotNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteBasicProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = "" };  // Empty string, not null

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert - Empty string is not null, so NullSubstitute shouldn't apply
        Assert.Equal("", result.Name);
    }

    [Fact]
    public void NullSubstitute_WithDecimal_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteDecimalProfile>());
        var mapper = config.CreateMapper();

        // Act
        var source = new NullSubSource { NullableDecimal = null };
        var result = mapper.Map<NullSubDestination>(source);

        // Assert
        Assert.Equal(0.0m, result.NullableDecimal);
    }

    [Fact]
    public void NullSubstitute_WithDateTime_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteDateProfile>());
        var mapper = config.CreateMapper();

        // Act - Null date
        var source1 = new NullSubSource { NullableDate = null };
        var result1 = mapper.Map<NullSubDestination>(source1);
        Assert.Equal(NullSubstituteDateProfile.DefaultDate, result1.NullableDate);

        // Act - Has date
        var existingDate = new DateTime(2024, 6, 15);
        var source2 = new NullSubSource { NullableDate = existingDate };
        var result2 = mapper.Map<NullSubDestination>(source2);
        Assert.Equal(existingDate, result2.NullableDate);
    }

    [Fact]
    public void NullSubstitute_MultipleMembers_EachHasOwnSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteMultipleProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = null, Title = null };

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert
        Assert.Equal("No Name", result.Name);
        Assert.Equal("No Title", result.Title);
    }

    [Fact]
    public void NullSubstitute_WithMapFromExpression_WorksTogether()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteWithMapFromProfile>());
        var mapper = config.CreateMapper();

        // Act - Inner is null
        var source1 = new NullSubSource { Inner = null };
        var result1 = mapper.Map<NullSubDestination>(source1);
        Assert.Equal("No Data", result1.InnerData);

        // Act - Inner.Data is null
        var source2 = new NullSubSource { Inner = new InnerSource { Data = null } };
        var result2 = mapper.Map<NullSubDestination>(source2);
        Assert.Equal("No Data", result2.InnerData);

        // Act - Inner.Data has value
        var source3 = new NullSubSource { Inner = new InnerSource { Data = "Hello" } };
        var result3 = mapper.Map<NullSubDestination>(source3);
        Assert.Equal("Hello", result3.InnerData);
    }

    [Fact]
    public void NullSubstitute_WithCondition_AppliesInCorrectOrder()
    {
        // Arrange - NullSubstitute is applied BEFORE Condition is evaluated
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteWithConditionProfile>());
        var mapper = config.CreateMapper();

        // Act - Source is null, substitute is "N/A" (length 3), condition passes
        var source = new NullSubSource { Name = null };
        var result = mapper.Map<NullSubDestination>(source);

        // Assert - "N/A" is mapped because after substitution, length is 3 > 1
        Assert.Equal("N/A", result.Name);
    }

    [Fact]
    public void NullSubstitute_WithPreCondition_NotAppliedIfPreConditionFails()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteWithPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = null, NullableInt = null };

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert - NullSubstitute not applied because PreCondition failed
        Assert.Null(result.Name);
    }

    [Fact]
    public void NullSubstitute_ZeroValueForNullableInt_IsNotNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteInt999Profile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { NullableInt = 0 };  // Zero, not null

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert - Zero is not null, so NullSubstitute shouldn't apply
        Assert.Equal(0, result.NullableInt);
    }

    [Fact]
    public void NullSubstitute_WithMappingToExistingDestination_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteSubstitutedProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = null };
        var destination = new NullSubDestination { Name = "Original" };

        // Act
        var result = mapper.Map(source, destination);

        // Assert - NullSubstitute should override the existing destination value
        Assert.Equal("Substituted", result.Name);
    }

    [Fact]
    public void NullSubstitute_WithAutoConventionMapping_Works()
    {
        // Arrange - Using convention (no explicit MapFrom)
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteAutoConventionProfile>());
        var mapper = config.CreateMapper();
        var source = new NullSubSource { Name = null };

        // Act
        var result = mapper.Map<NullSubDestination>(source);

        // Assert
        Assert.Equal("Default", result.Name);
    }
}
