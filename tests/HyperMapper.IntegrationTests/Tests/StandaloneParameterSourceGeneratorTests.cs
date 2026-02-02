using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

/// <summary>
/// v12.1.0: Tests for standalone lambda parameters.
/// Verifies expressions like "s => s" (identity) and standalone parameter references work correctly.
/// This tests the enhanced ReplaceParameterWithSource() in MapperGenerator.
/// </summary>
public class StandaloneParameterSourceGeneratorTests
{
    #region Test Models

    public class SimpleSource
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class SimpleDestination
    {
        public string Name { get; set; } = "";
        public SimpleSource? OriginalSource { get; set; }
        public string NameOrDefault { get; set; } = "";
        public bool HasValue { get; set; }
    }

    public class WrapperSource
    {
        public SimpleSource? Inner { get; set; }
        public string Label { get; set; } = "";
    }

    public class WrapperDestination
    {
        public SimpleSource? UnwrappedInner { get; set; }
        public string Label { get; set; } = "";
        public string InnerNameOrNA { get; set; } = "";
    }

    public class NullableValueSource
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
    }

    public class NullableValueDestination
    {
        public int ValueOrZero { get; set; }
        public string ValueOrEmpty { get; set; } = "";
        public string Description { get; set; } = "";
    }

    #endregion

    #region Profiles

    public class StandaloneParameterProfile : Profile
    {
        public StandaloneParameterProfile()
        {
            // Test: Standalone parameter in ternary expression
            CreateMap<SimpleSource, SimpleDestination>()
                .ForMember(d => d.NameOrDefault, opt => opt.MapFrom(s =>
                    string.IsNullOrEmpty(s.Name) ? "Default" : s.Name))
                .ForMember(d => d.HasValue, opt => opt.MapFrom(s => s.Value > 0));
        }
    }

    public class WrapperUnwrapProfile : Profile
    {
        public WrapperUnwrapProfile()
        {
            // Test: Unwrapping nested object (s => s.Inner returns the inner object)
            CreateMap<WrapperSource, WrapperDestination>()
                .ForMember(d => d.UnwrappedInner, opt => opt.MapFrom(s => s.Inner))
                .ForMember(d => d.InnerNameOrNA, opt => opt.MapFrom(s =>
                    s.Inner == null ? "N/A" : s.Inner.Name));
        }
    }

    public class NullCoalescingProfile : Profile
    {
        public NullCoalescingProfile()
        {
            // Test: Null coalescing with standalone parameter
            CreateMap<NullableValueSource, NullableValueDestination>()
                .ForMember(d => d.ValueOrZero, opt => opt.MapFrom(s => s.NullableInt ?? 0))
                .ForMember(d => d.ValueOrEmpty, opt => opt.MapFrom(s => s.NullableString ?? ""))
                .ForMember(d => d.Description, opt => opt.MapFrom(s =>
                    s.NullableInt.HasValue
                        ? $"Value: {s.NullableInt.Value}"
                        : "No value"));
        }
    }

    #endregion

    #region Ternary Expression Tests

    [Fact]
    public void CodeGen_TernaryWithParameter_ShouldReplaceCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StandaloneParameterProfile>());
        var mapper = config.CreateMapper();

        var source = new SimpleSource
        {
            Name = "Test",
            Value = 10
        };

        // Act
        var result = mapper.Map<SimpleDestination>(source);

        // Assert
        Assert.Equal("Test", result.NameOrDefault);
        Assert.True(result.HasValue);
    }

    [Fact]
    public void CodeGen_TernaryWithEmptyString_ShouldReturnDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StandaloneParameterProfile>());
        var mapper = config.CreateMapper();

        var source = new SimpleSource
        {
            Name = "",
            Value = 0
        };

        // Act
        var result = mapper.Map<SimpleDestination>(source);

        // Assert
        Assert.Equal("Default", result.NameOrDefault);
        Assert.False(result.HasValue);
    }

    #endregion

    #region Null Conditional Tests

    [Fact]
    public void CodeGen_NullCheck_WithNestedObject_ShouldWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<WrapperUnwrapProfile>());
        var mapper = config.CreateMapper();

        var source = new WrapperSource
        {
            Inner = new SimpleSource { Name = "InnerName", Value = 42 },
            Label = "Wrapper"
        };

        // Act
        var result = mapper.Map<WrapperDestination>(source);

        // Assert
        Assert.NotNull(result.UnwrappedInner);
        Assert.Equal("InnerName", result.UnwrappedInner!.Name);
        Assert.Equal("InnerName", result.InnerNameOrNA);
    }

    [Fact]
    public void CodeGen_NullCheck_WithNullNestedObject_ShouldReturnNA()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<WrapperUnwrapProfile>());
        var mapper = config.CreateMapper();

        var source = new WrapperSource
        {
            Inner = null,
            Label = "Empty Wrapper"
        };

        // Act
        var result = mapper.Map<WrapperDestination>(source);

        // Assert
        Assert.Null(result.UnwrappedInner);
        Assert.Equal("N/A", result.InnerNameOrNA);
    }

    #endregion

    #region Null Coalescing Tests

    [Fact]
    public void CodeGen_NullCoalescing_WithValue_ShouldReturnValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableValueSource
        {
            NullableInt = 42,
            NullableString = "Hello"
        };

        // Act
        var result = mapper.Map<NullableValueDestination>(source);

        // Assert
        Assert.Equal(42, result.ValueOrZero);
        Assert.Equal("Hello", result.ValueOrEmpty);
        Assert.Equal("Value: 42", result.Description);
    }

    [Fact]
    public void CodeGen_NullCoalescing_WithNull_ShouldReturnDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();

        var source = new NullableValueSource
        {
            NullableInt = null,
            NullableString = null
        };

        // Act
        var result = mapper.Map<NullableValueDestination>(source);

        // Assert
        Assert.Equal(0, result.ValueOrZero);
        Assert.Equal("", result.ValueOrEmpty);
        Assert.Equal("No value", result.Description);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void CodeGen_StandaloneParameter_WithCollection_ShouldMapAllItems()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StandaloneParameterProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<SimpleSource>
        {
            new() { Name = "First", Value = 1 },
            new() { Name = "", Value = 0 },
            new() { Name = "Third", Value = 3 }
        };

        // Act
        var results = mapper.Map<List<SimpleDestination>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].NameOrDefault);
        Assert.Equal("Default", results[1].NameOrDefault);
        Assert.Equal("Third", results[2].NameOrDefault);
        Assert.True(results[0].HasValue);
        Assert.False(results[1].HasValue);
        Assert.True(results[2].HasValue);
    }

    [Fact]
    public void CodeGen_NullCoalescing_WithCollection_ShouldMapAllItems()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<NullableValueSource>
        {
            new() { NullableInt = 10, NullableString = "A" },
            new() { NullableInt = null, NullableString = null },
            new() { NullableInt = 30, NullableString = "C" }
        };

        // Act
        var results = mapper.Map<List<NullableValueDestination>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(10, results[0].ValueOrZero);
        Assert.Equal(0, results[1].ValueOrZero);
        Assert.Equal(30, results[2].ValueOrZero);
        Assert.Equal("A", results[0].ValueOrEmpty);
        Assert.Equal("", results[1].ValueOrEmpty);
        Assert.Equal("C", results[2].ValueOrEmpty);
    }

    #endregion
}
