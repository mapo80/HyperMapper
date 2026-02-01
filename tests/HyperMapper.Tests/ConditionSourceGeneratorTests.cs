using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v8.1.0: Unit tests for Condition() in Source Generator.
/// Tests post-mapping conditional logic at compile-time code generation.
/// </summary>
public class ConditionSourceGeneratorTests
{
    #region Test Types

    public class SourceWithValue
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithValue
    {
        public int Value { get; set; }
        public string Name { get; set; } = "";
    }

    public class SourceWithNullableValue
    {
        public int? OptionalValue { get; set; }
    }

    public class DestWithOptionalValue
    {
        public int OptionalValue { get; set; }
    }

    public class SourceWithMultipleConditions
    {
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public bool IsVerified { get; set; }
    }

    public class DestWithMultipleConditions
    {
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public bool IsVerified { get; set; }
    }

    public class SourceWithIsActive
    {
        public bool IsActive { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithNameOnly
    {
        public string Name { get; set; } = "";
    }

    #endregion

    #region Test Profiles

    public class SimpleConditionProfile : Profile
    {
        public SimpleConditionProfile()
        {
            CreateMap<SourceWithValue, DestWithValue>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.Condition((src, dest, srcMember) => srcMember > 0);
                })
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name));
        }
    }

    public class ConditionFalseProfile : Profile
    {
        public ConditionFalseProfile()
        {
            CreateMap<SourceWithValue, DestWithValue>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.Condition((src, dest, srcMember) => srcMember > 100); // Will fail for most test values
                })
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name));
        }
    }

    public class NullableConditionProfile : Profile
    {
        public NullableConditionProfile()
        {
            CreateMap<SourceWithNullableValue, DestWithOptionalValue>()
                .ForMember(d => d.OptionalValue, opt =>
                {
                    opt.MapFrom(s => s.OptionalValue ?? 0);
                    opt.Condition((src, dest, srcMember) => srcMember > 0);
                });
        }
    }

    public class MultipleConditionsProfile : Profile
    {
        public MultipleConditionsProfile()
        {
            CreateMap<SourceWithMultipleConditions, DestWithMultipleConditions>()
                .ForMember(d => d.Age, opt =>
                {
                    opt.MapFrom(s => s.Age);
                    opt.Condition((src, dest, srcMember) => srcMember >= 18);
                })
                .ForMember(d => d.Salary, opt =>
                {
                    opt.MapFrom(s => s.Salary);
                    opt.Condition((src, dest, srcMember) => srcMember > 0);
                })
                .ForMember(d => d.IsVerified, opt => opt.MapFrom(s => s.IsVerified));
        }
    }

    public class ConditionWithPreConditionProfile : Profile
    {
        public ConditionWithPreConditionProfile()
        {
            CreateMap<SourceWithIsActive, DestWithNameOnly>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.PreCondition(s => s.IsActive);
                    opt.MapFrom(s => s.Name ?? "Default");
                    opt.Condition((src, dest, srcMember) => !string.IsNullOrEmpty(srcMember));
                });
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void Condition_WhenTrue_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithValue { Value = 42, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Value); // Condition (value > 0) is true
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Condition_WhenFalse_SkipsMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithValue { Value = -5, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(0, dest.Value); // Condition (value > 0) is false, keeps default
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Condition_WithZeroValue_SkipsMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithValue { Value = 0, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(0, dest.Value); // Condition (value > 0) is false (0 is not > 0)
    }

    [Fact]
    public void Condition_NumericComparison_GreaterThan()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionFalseProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithValue { Value = 50, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(0, dest.Value); // Condition (value > 100) is false (50 is not > 100)
    }

    [Fact]
    public void Condition_NumericComparison_PassesWhenGreater()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionFalseProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithValue { Value = 150, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(150, dest.Value); // Condition (value > 100) is true (150 > 100)
    }

    [Fact]
    public void Condition_NullableWithMapFrom_EvaluatesTransformedValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableValue { OptionalValue = 10 };

        // Act
        var dest = mapper.Map<DestWithOptionalValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.OptionalValue); // MapFrom transforms, then Condition checks
    }

    [Fact]
    public void Condition_NullableNull_SkipsMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableValue { OptionalValue = null };

        // Act
        var dest = mapper.Map<DestWithOptionalValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(0, dest.OptionalValue); // null ?? 0 = 0, then condition (0 > 0) is false
    }

    [Fact]
    public void Condition_MultipleMembers_IndependentConditions()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleConditionsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMultipleConditions
        {
            Age = 25,      // Will pass (>= 18)
            Salary = -100, // Will fail (not > 0)
            IsVerified = true
        };

        // Act
        var dest = mapper.Map<DestWithMultipleConditions>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(25, dest.Age);       // Condition passed
        Assert.Equal(0, dest.Salary);     // Condition failed, keeps default
        Assert.True(dest.IsVerified);     // No condition, always maps
    }

    [Fact]
    public void Condition_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleConditionProfile>());
        var mapper = config.CreateMapper();
        SourceWithValue? nullSource = null;

        // Act
        var dest = mapper.Map<DestWithValue>(nullSource!);

        // Assert
        Assert.Null(dest);
    }

    #endregion
}
