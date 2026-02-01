using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v8.1.0: Unit tests for NullSubstitute() in Source Generator.
/// Tests null value substitution at compile-time code generation.
/// </summary>
public class NullSubstituteSourceGeneratorTests
{
    #region Test Types

    public class SourceWithNullableString
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class DestWithString
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class SourceWithNullableInt
    {
        public int? Value { get; set; }
        public int? Count { get; set; }
    }

    public class DestWithInt
    {
        public int Value { get; set; }
        public int Count { get; set; }
    }

    public class SourceWithNullableDecimal
    {
        public decimal? Amount { get; set; }
    }

    public class DestWithDecimal
    {
        public decimal Amount { get; set; }
    }

    public class SourceWithMultipleNullables
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public decimal? Salary { get; set; }
    }

    public class DestWithMultipleValues
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Salary { get; set; }
    }

    public class SourceWithIsActive
    {
        public bool IsActive { get; set; }
        public string? Name { get; set; }
    }

    public class DestWithName
    {
        public string Name { get; set; } = "";
    }

    #endregion

    #region Test Profiles

    public class StringNullSubstituteProfile : Profile
    {
        public StringNullSubstituteProfile()
        {
            CreateMap<SourceWithNullableString, DestWithString>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("N/A");
                })
                .ForMember(d => d.Description, opt =>
                {
                    opt.MapFrom(s => s.Description);
                    opt.NullSubstitute("No description");
                });
        }
    }

    public class IntNullSubstituteProfile : Profile
    {
        public IntNullSubstituteProfile()
        {
            CreateMap<SourceWithNullableInt, DestWithInt>()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.NullSubstitute(-1);
                })
                .ForMember(d => d.Count, opt =>
                {
                    opt.MapFrom(s => s.Count);
                    opt.NullSubstitute(0);
                });
        }
    }

    public class DecimalNullSubstituteProfile : Profile
    {
        public DecimalNullSubstituteProfile()
        {
            CreateMap<SourceWithNullableDecimal, DestWithDecimal>()
                .ForMember(d => d.Amount, opt =>
                {
                    opt.MapFrom(s => s.Amount);
                    opt.NullSubstitute(0.0m);
                });
        }
    }

    public class MultipleNullSubstituteProfile : Profile
    {
        public MultipleNullSubstituteProfile()
        {
            CreateMap<SourceWithMultipleNullables, DestWithMultipleValues>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("Unknown");
                })
                .ForMember(d => d.Age, opt =>
                {
                    opt.MapFrom(s => s.Age);
                    opt.NullSubstitute(0);
                })
                .ForMember(d => d.Salary, opt =>
                {
                    opt.MapFrom(s => s.Salary);
                    opt.NullSubstitute(0.0m);
                });
        }
    }

    public class NullSubstituteWithPreConditionProfile : Profile
    {
        public NullSubstituteWithPreConditionProfile()
        {
            CreateMap<SourceWithIsActive, DestWithName>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.PreCondition(s => s.IsActive);
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("Active User");
                });
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void NullSubstitute_StringProperty_WhenNull_ReturnsSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableString { Name = null, Description = null };

        // Act
        var dest = mapper.Map<DestWithString>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("N/A", dest.Name);
        Assert.Equal("No description", dest.Description);
    }

    [Fact]
    public void NullSubstitute_StringProperty_WhenNotNull_ReturnsOriginal()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableString { Name = "John", Description = "Test description" };

        // Act
        var dest = mapper.Map<DestWithString>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.Name);
        Assert.Equal("Test description", dest.Description);
    }

    [Fact]
    public void NullSubstitute_NullableInt_WhenNull_ReturnsSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IntNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableInt { Value = null, Count = null };

        // Act
        var dest = mapper.Map<DestWithInt>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(-1, dest.Value);
        Assert.Equal(0, dest.Count);
    }

    [Fact]
    public void NullSubstitute_NullableDecimal_WhenNull_ReturnsSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DecimalNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableDecimal { Amount = null };

        // Act
        var dest = mapper.Map<DestWithDecimal>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(0.0m, dest.Amount);
    }

    [Fact]
    public void NullSubstitute_EmptyString_NotTreatedAsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableString { Name = "", Description = "" };

        // Act
        var dest = mapper.Map<DestWithString>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("", dest.Name); // Empty string is NOT null, so NullSubstitute doesn't apply
        Assert.Equal("", dest.Description);
    }

    [Fact]
    public void NullSubstitute_ZeroValue_NotTreatedAsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IntNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNullableInt { Value = 0, Count = 0 };

        // Act
        var dest = mapper.Map<DestWithInt>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(0, dest.Value); // Zero is NOT null, so NullSubstitute doesn't apply (would be -1 if null)
        Assert.Equal(0, dest.Count);
    }

    [Fact]
    public void NullSubstitute_MultipleMembers_EachHasOwnSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMultipleNullables { Name = null, Age = null, Salary = null };

        // Act
        var dest = mapper.Map<DestWithMultipleValues>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Unknown", dest.Name);
        Assert.Equal(0, dest.Age);
        Assert.Equal(0.0m, dest.Salary);
    }

    [Fact]
    public void NullSubstitute_WithPreCondition_WhenPreConditionTrue_AppliesSubstitute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteWithPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithIsActive { IsActive = true, Name = null };

        // Act
        var dest = mapper.Map<DestWithName>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Active User", dest.Name);
    }

    [Fact]
    public void NullSubstitute_WithPreCondition_WhenPreConditionFalse_SkipsMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteWithPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithIsActive { IsActive = false, Name = "Test" };

        // Act
        var dest = mapper.Map<DestWithName>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("", dest.Name); // PreCondition failed, so no mapping (default value)
    }

    [Fact]
    public void NullSubstitute_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringNullSubstituteProfile>());
        var mapper = config.CreateMapper();
        SourceWithNullableString? nullSource = null;

        // Act
        var dest = mapper.Map<DestWithString>(nullSource!);

        // Assert
        Assert.Null(dest);
    }

    #endregion
}
