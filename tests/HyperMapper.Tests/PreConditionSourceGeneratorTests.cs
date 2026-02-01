using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for Source Generator support of PreCondition.
/// These tests verify that PreCondition expressions are compiled at build-time.
/// </summary>
public class PreConditionSourceGeneratorTests
{
    #region Test Types

    public class SourceWithCondition
    {
        public bool IsActive { get; set; }
        public string? Value { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithCondition
    {
        public string? Value { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithNullCheck
    {
        public string? OptionalValue { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithNullCheck
    {
        public string? OptionalValue { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithComparison
    {
        public int Age { get; set; }
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithComparison
    {
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public enum Status { Inactive, Active, Pending }

    public class SourceWithEnum
    {
        public Status Status { get; set; }
        public string? PremiumContent { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithEnum
    {
        public string? PremiumContent { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithStringCheck
    {
        public string? Type { get; set; }
        public string? SpecialValue { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithStringCheck
    {
        public string? SpecialValue { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithAnd
    {
        public bool IsActive { get; set; }
        public int Age { get; set; }
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithAnd
    {
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithOr
    {
        public bool IsActive { get; set; }
        public bool IsPremium { get; set; }
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithOr
    {
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithNot
    {
        public bool IsDisabled { get; set; }
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithNot
    {
        public string? Content { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithMapFrom
    {
        public bool ShouldMap { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithMapFrom
    {
        public string? FullName { get; set; }
        public string? Name { get; set; }
    }

    public class SourceWithMultiplePreConditions
    {
        public bool ShowFirst { get; set; }
        public bool ShowSecond { get; set; }
        public string? First { get; set; }
        public string? Second { get; set; }
        public string? Always { get; set; }
    }

    public class DestWithMultiplePreConditions
    {
        public string? First { get; set; }
        public string? Second { get; set; }
        public string? Always { get; set; }
    }

    #endregion

    #region Test Profiles

    public class SimpleBoolPreConditionProfile : Profile
    {
        public SimpleBoolPreConditionProfile()
        {
            CreateMap<SourceWithCondition, DestWithCondition>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.PreCondition(s => s.IsActive);
                    opt.MapFrom(s => s.Value);
                });
        }
    }

    public class NullCheckPreConditionProfile : Profile
    {
        public NullCheckPreConditionProfile()
        {
            CreateMap<SourceWithNullCheck, DestWithNullCheck>()
                .ForMember(d => d.OptionalValue, opt =>
                {
                    opt.PreCondition(s => s.OptionalValue != null);
                    opt.MapFrom(s => s.OptionalValue);
                });
        }
    }

    public class ComparisonPreConditionProfile : Profile
    {
        public ComparisonPreConditionProfile()
        {
            CreateMap<SourceWithComparison, DestWithComparison>()
                .ForMember(d => d.Content, opt =>
                {
                    opt.PreCondition(s => s.Age > 18);
                    opt.MapFrom(s => s.Content);
                });
        }
    }

    public class EnumCheckPreConditionProfile : Profile
    {
        public EnumCheckPreConditionProfile()
        {
            CreateMap<SourceWithEnum, DestWithEnum>()
                .ForMember(d => d.PremiumContent, opt =>
                {
                    opt.PreCondition(s => s.Status == Status.Active);
                    opt.MapFrom(s => s.PremiumContent);
                });
        }
    }

    public class StringComparisonPreConditionProfile : Profile
    {
        public StringComparisonPreConditionProfile()
        {
            CreateMap<SourceWithStringCheck, DestWithStringCheck>()
                .ForMember(d => d.SpecialValue, opt =>
                {
                    opt.PreCondition(s => s.Type == "Premium");
                    opt.MapFrom(s => s.SpecialValue);
                });
        }
    }

    public class AndOperatorPreConditionProfile : Profile
    {
        public AndOperatorPreConditionProfile()
        {
            CreateMap<SourceWithAnd, DestWithAnd>()
                .ForMember(d => d.Content, opt =>
                {
                    opt.PreCondition(s => s.IsActive && s.Age > 18);
                    opt.MapFrom(s => s.Content);
                });
        }
    }

    public class OrOperatorPreConditionProfile : Profile
    {
        public OrOperatorPreConditionProfile()
        {
            CreateMap<SourceWithOr, DestWithOr>()
                .ForMember(d => d.Content, opt =>
                {
                    opt.PreCondition(s => s.IsActive || s.IsPremium);
                    opt.MapFrom(s => s.Content);
                });
        }
    }

    public class NotOperatorPreConditionProfile : Profile
    {
        public NotOperatorPreConditionProfile()
        {
            CreateMap<SourceWithNot, DestWithNot>()
                .ForMember(d => d.Content, opt =>
                {
                    opt.PreCondition(s => !s.IsDisabled);
                    opt.MapFrom(s => s.Content);
                });
        }
    }

    public class PreConditionWithMapFromProfile : Profile
    {
        public PreConditionWithMapFromProfile()
        {
            CreateMap<SourceWithMapFrom, DestWithMapFrom>()
                .ForMember(d => d.FullName, opt =>
                {
                    opt.PreCondition(s => s.ShouldMap);
                    opt.MapFrom(s => $"{s.FirstName} {s.LastName}");
                });
        }
    }

    public class MultiplePreConditionsProfile : Profile
    {
        public MultiplePreConditionsProfile()
        {
            CreateMap<SourceWithMultiplePreConditions, DestWithMultiplePreConditions>()
                .ForMember(d => d.First, opt =>
                {
                    opt.PreCondition(s => s.ShowFirst);
                    opt.MapFrom(s => s.First);
                })
                .ForMember(d => d.Second, opt =>
                {
                    opt.PreCondition(s => s.ShowSecond);
                    opt.MapFrom(s => s.Second);
                });
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void PreCondition_SimpleBoolProperty_WhenTrue_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoolPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { IsActive = true, Value = "Active Value", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithCondition>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Active Value", dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void PreCondition_SimpleBoolProperty_WhenFalse_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoolPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { IsActive = false, Value = "Should Not Map", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithCondition>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.Value);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void PreCondition_NullCheck_WhenNotNull_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCheckPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullCheck { OptionalValue = "Has Value", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithNullCheck>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Has Value", dest.OptionalValue);
    }

    [Fact]
    public void PreCondition_NullCheck_WhenNull_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCheckPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullCheck { OptionalValue = null, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithNullCheck>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.OptionalValue);
    }

    [Fact]
    public void PreCondition_Comparison_WhenConditionMet_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ComparisonPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithComparison { Age = 21, Content = "Adult Content", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithComparison>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Adult Content", dest.Content);
    }

    [Fact]
    public void PreCondition_Comparison_WhenConditionNotMet_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ComparisonPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithComparison { Age = 16, Content = "Should Not Map", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithComparison>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.Content);
    }

    [Fact]
    public void PreCondition_EnumCheck_WhenEnumMatches_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumCheckPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithEnum { Status = Status.Active, PremiumContent = "Premium", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithEnum>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Premium", dest.PremiumContent);
    }

    [Fact]
    public void PreCondition_EnumCheck_WhenEnumDoesNotMatch_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EnumCheckPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithEnum { Status = Status.Inactive, PremiumContent = "Should Not Map", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithEnum>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.PremiumContent);
    }

    [Fact]
    public void PreCondition_AndOperator_WhenBothTrue_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AndOperatorPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithAnd { IsActive = true, Age = 21, Content = "Content", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithAnd>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Content", dest.Content);
    }

    [Fact]
    public void PreCondition_AndOperator_WhenOneFalse_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AndOperatorPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithAnd { IsActive = true, Age = 16, Content = "Should Not Map", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithAnd>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.Content);
    }

    [Fact]
    public void PreCondition_OrOperator_WhenOneTrue_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrOperatorPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithOr { IsActive = false, IsPremium = true, Content = "Content", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithOr>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Content", dest.Content);
    }

    [Fact]
    public void PreCondition_OrOperator_WhenBothFalse_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrOperatorPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithOr { IsActive = false, IsPremium = false, Content = "Should Not Map", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithOr>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.Content);
    }

    [Fact]
    public void PreCondition_NotOperator_WhenNotDisabled_MapsValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NotOperatorPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNot { IsDisabled = false, Content = "Content", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithNot>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Content", dest.Content);
    }

    [Fact]
    public void PreCondition_NotOperator_WhenDisabled_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NotOperatorPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNot { IsDisabled = true, Content = "Should Not Map", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithNot>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.Content);
    }

    [Fact]
    public void PreCondition_WithMapFrom_WhenTrue_AppliesBothCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreConditionWithMapFromProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMapFrom { ShouldMap = true, FirstName = "John", LastName = "Doe", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithMapFrom>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void PreCondition_WithMapFrom_WhenFalse_DoesNotMapValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreConditionWithMapFromProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMapFrom { ShouldMap = false, FirstName = "John", LastName = "Doe", Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithMapFrom>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.FullName);
    }

    [Fact]
    public void PreCondition_MultipleProperties_EachHasOwnCondition()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultiplePreConditionsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMultiplePreConditions
        {
            ShowFirst = true,
            ShowSecond = false,
            First = "First Value",
            Second = "Second Value",
            Always = "Always Mapped"
        };

        // Act
        var dest = mapper.Map<DestWithMultiplePreConditions>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("First Value", dest.First);
        Assert.Null(dest.Second);
        Assert.Equal("Always Mapped", dest.Always);
    }

    #endregion
}
