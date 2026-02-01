using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 Condition() feature - post-mapping condition.
/// AutoMapper API compatibility: Condition((src, dest, srcMember) => ...)
/// </summary>
public class ConditionTests
{
    #region Test Models

    public class ConditionSource
    {
        public int Value { get; set; }
        public string? Name { get; set; }
        public int? NullableValue { get; set; }
        public decimal Amount { get; set; }
    }

    public class ConditionDestination
    {
        public int Value { get; set; }
        public string? Name { get; set; }
        public int? NullableValue { get; set; }
        public decimal Amount { get; set; }
    }

    public class SourceWithNested
    {
        public int Id { get; set; }
        public InnerSource? Inner { get; set; }
    }

    public class InnerSource
    {
        public string? Data { get; set; }
    }

    public class DestinationWithNested
    {
        public int Id { get; set; }
        public string? InnerData { get; set; }
    }

    #endregion

    #region Profiles

    public class ConditionWhenTrueProfile : Profile
    {
        public ConditionWhenTrueProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.Condition((src, dest, srcMember) => srcMember > 0);
                });
        }
    }

    public class ConditionWithNullSourceProfile : Profile
    {
        public ConditionWithNullSourceProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.Condition((src, dest, srcMember) => srcMember != null);
                });
        }
    }

    public class ConditionWithDestinationValueProfile : Profile
    {
        public ConditionWithDestinationValueProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    // Only map if destination's current value is 0
                    opt.Condition((src, dest, srcMember) => dest.Value == 0);
                });
        }
    }

    public class CombinedConditionProfile : Profile
    {
        public static bool PreConditionCalled { get; set; }
        public static bool ConditionCalled { get; set; }

        public CombinedConditionProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.PreCondition(s =>
                    {
                        PreConditionCalled = true;
                        return s.Value >= 0;
                    });
                    opt.Condition((src, dest, srcMember) =>
                    {
                        ConditionCalled = true;
                        return srcMember > 5;
                    });
                });
        }
    }

    public class PreConditionFailsProfile : Profile
    {
        public static bool ConditionCalled { get; set; }

        public PreConditionFailsProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.PreCondition(s => s.Value >= 0);  // This will fail for negative values
                    opt.Condition((src, dest, srcMember) =>
                    {
                        ConditionCalled = true;
                        return true;
                    });
                });
        }
    }

    public class ConditionWithResolutionContextProfile : Profile
    {
        public static bool ContextUsed { get; set; }

        public ConditionWithResolutionContextProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.Condition((src, dest, srcMember, context) =>
                    {
                        ContextUsed = context != null;
                        return srcMember > 0;
                    });
                });
        }
    }

    public class ComplexConditionProfile : Profile
    {
        public ComplexConditionProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Amount, opt =>
                {
                    opt.MapFrom(s => s.Amount);
                    opt.Condition((src, dest, srcMember) =>
                        srcMember > 0 &&
                        srcMember < 1000 &&
                        src.Value > 0);
                });
        }
    }

    public class MultipleMembersConditionProfile : Profile
    {
        public MultipleMembersConditionProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.Condition((src, dest, srcMember) => srcMember > 0);
                })
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.Condition((src, dest, srcMember) => !string.IsNullOrEmpty(srcMember));
                });
        }
    }

    public class NullableConditionProfile : Profile
    {
        public NullableConditionProfile()
        {
            CreateMap<ConditionSource, ConditionDestination>()
                .ForMember(d => d.NullableValue, opt =>
                {
                    opt.MapFrom(s => s.NullableValue);
                    opt.Condition((src, dest, srcMember) => srcMember.HasValue && srcMember.Value > 0);
                });
        }
    }

    public class MapFromConditionProfile : Profile
    {
        public MapFromConditionProfile()
        {
            CreateMap<SourceWithNested, DestinationWithNested>()
                .ForMember(d => d.InnerData, opt =>
                {
                    opt.MapFrom(s => s.Inner != null ? s.Inner.Data : null);
                    opt.Condition((src, dest, srcMember) => srcMember != null && srcMember.Length > 2);
                });
        }
    }

    #endregion

    [Fact]
    public void Condition_WhenTrue_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionWhenTrueProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = 10 };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Condition_WhenFalse_SkipsMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionWhenTrueProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = -5 };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert - Value should remain default (0) because condition is false
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Condition_WithNullSourceValue_HandlesGracefully()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionWithNullSourceProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Name = null };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert
        Assert.Null(result.Name);
    }

    [Fact]
    public void Condition_WithDestinationValue_EvaluatesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionWithDestinationValueProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = 100 };
        var destination = new ConditionDestination { Value = 50 };

        // Act
        var result = mapper.Map(source, destination);

        // Assert - Value should remain 50 because destination already had a value
        Assert.Equal(50, result.Value);
    }

    [Fact]
    public void Condition_CombinedWithPreCondition_BothEvaluated()
    {
        // Arrange
        CombinedConditionProfile.PreConditionCalled = false;
        CombinedConditionProfile.ConditionCalled = false;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<CombinedConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = 10 };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert
        Assert.True(CombinedConditionProfile.PreConditionCalled, "PreCondition should have been called");
        Assert.True(CombinedConditionProfile.ConditionCalled, "Condition should have been called");
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Condition_PreConditionFails_ConditionNotEvaluated()
    {
        // Arrange
        PreConditionFailsProfile.ConditionCalled = false;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreConditionFailsProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = -1 };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert
        Assert.False(PreConditionFailsProfile.ConditionCalled, "Condition should not have been called when PreCondition fails");
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Condition_WithResolutionContext_HasAccess()
    {
        // Arrange
        ConditionWithResolutionContextProfile.ContextUsed = false;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionWithResolutionContextProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = 10 };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert
        Assert.True(ConditionWithResolutionContextProfile.ContextUsed, "ResolutionContext should have been provided");
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Condition_WithComplexExpression_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ComplexConditionProfile>());
        var mapper = config.CreateMapper();

        // Act - All conditions true
        var source1 = new ConditionSource { Value = 10, Amount = 500 };
        var result1 = mapper.Map<ConditionDestination>(source1);
        Assert.Equal(500, result1.Amount);

        // Act - Amount out of range
        var source2 = new ConditionSource { Value = 10, Amount = 1500 };
        var result2 = mapper.Map<ConditionDestination>(source2);
        Assert.Equal(0, result2.Amount);

        // Act - Value is 0
        var source3 = new ConditionSource { Value = 0, Amount = 500 };
        var result3 = mapper.Map<ConditionDestination>(source3);
        Assert.Equal(0, result3.Amount);
    }

    [Fact]
    public void Condition_MultipleMembers_IndependentConditions()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleMembersConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new ConditionSource { Value = -1, Name = "Test" };

        // Act
        var result = mapper.Map<ConditionDestination>(source);

        // Assert - Value not mapped (condition false), Name mapped (condition true)
        Assert.Equal(0, result.Value);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void Condition_WithNullableType_HandlesNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableConditionProfile>());
        var mapper = config.CreateMapper();

        // Act - Null value
        var source1 = new ConditionSource { NullableValue = null };
        var result1 = mapper.Map<ConditionDestination>(source1);
        Assert.Null(result1.NullableValue);

        // Act - Has value but <= 0
        var source2 = new ConditionSource { NullableValue = -5 };
        var result2 = mapper.Map<ConditionDestination>(source2);
        Assert.Null(result2.NullableValue);

        // Act - Has value > 0
        var source3 = new ConditionSource { NullableValue = 10 };
        var result3 = mapper.Map<ConditionDestination>(source3);
        Assert.Equal(10, result3.NullableValue);
    }

    [Fact]
    public void Condition_OnMapFromExpression_WorksTogether()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MapFromConditionProfile>());
        var mapper = config.CreateMapper();

        // Act - Inner data is long enough
        var source1 = new SourceWithNested { Inner = new InnerSource { Data = "Hello" } };
        var result1 = mapper.Map<DestinationWithNested>(source1);
        Assert.Equal("Hello", result1.InnerData);

        // Act - Inner data too short
        var source2 = new SourceWithNested { Inner = new InnerSource { Data = "Hi" } };
        var result2 = mapper.Map<DestinationWithNested>(source2);
        Assert.Null(result2.InnerData);
    }
}
