using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for Source Generator support of lambda converters.
/// These tests verify that ConvertUsing lambda expressions are inlined at compile-time.
/// </summary>
public class ConverterSourceGeneratorTests
{
    #region Test Types

    public class SourceForSimpleTransform
    {
        public int Value { get; set; }
    }

    public class DestForSimpleTransform
    {
        public int DoubleValue { get; set; }
    }

    public class SourceForPropertyAccess
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class DestForPropertyAccess
    {
        public string? FullName { get; set; }
    }

    public class SourceForTernary
    {
        public bool IsActive { get; set; }
        public string? Name { get; set; }
    }

    public class DestForTernary
    {
        public string? Status { get; set; }
    }

    public class SourceForNullCoalescing
    {
        public string? Value { get; set; }
    }

    public class DestForNullCoalescing
    {
        public string? ValueOrDefault { get; set; }
    }

    public class SourceForInterpolation
    {
        public string? First { get; set; }
        public string? Last { get; set; }
        public int Age { get; set; }
    }

    public class DestForInterpolation
    {
        public string? Description { get; set; }
    }

    public class SourceNullable
    {
        public int Value { get; set; }
    }

    public class DestNullable
    {
        public int TransformedValue { get; set; }
    }

    #endregion

    #region Test Profiles

    public class SimpleTransformProfile : Profile
    {
        public SimpleTransformProfile()
        {
            CreateMap<SourceForSimpleTransform, DestForSimpleTransform>()
                .ConvertUsing(s => new DestForSimpleTransform { DoubleValue = s.Value * 2 });
        }
    }

    public class PropertyAccessProfile : Profile
    {
        public PropertyAccessProfile()
        {
            CreateMap<SourceForPropertyAccess, DestForPropertyAccess>()
                .ConvertUsing(s => new DestForPropertyAccess { FullName = s.FirstName + " " + s.LastName });
        }
    }

    public class TernaryConverterProfile : Profile
    {
        public TernaryConverterProfile()
        {
            CreateMap<SourceForTernary, DestForTernary>()
                .ConvertUsing(s => new DestForTernary { Status = s.IsActive ? "Active" : "Inactive" });
        }
    }

    public class NullCoalescingProfile : Profile
    {
        public NullCoalescingProfile()
        {
            CreateMap<SourceForNullCoalescing, DestForNullCoalescing>()
                .ConvertUsing(s => new DestForNullCoalescing { ValueOrDefault = s.Value ?? "default" });
        }
    }

    public class InterpolationProfile : Profile
    {
        public InterpolationProfile()
        {
            CreateMap<SourceForInterpolation, DestForInterpolation>()
                .ConvertUsing(s => new DestForInterpolation { Description = $"{s.First} {s.Last}, Age: {s.Age}" });
        }
    }

    public class NullableSourceProfile : Profile
    {
        public NullableSourceProfile()
        {
            CreateMap<SourceNullable, DestNullable>()
                .ConvertUsing(s => new DestNullable { TransformedValue = s.Value + 100 });
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void LambdaConverter_SimpleTransform_InlinesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForSimpleTransform { Value = 21 };

        // Act
        var dest = mapper.Map<DestForSimpleTransform>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.DoubleValue);
    }

    [Fact]
    public void LambdaConverter_WithPropertyAccess_InlinesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PropertyAccessProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForPropertyAccess { FirstName = "John", LastName = "Doe" };

        // Act
        var dest = mapper.Map<DestForPropertyAccess>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void LambdaConverter_WithTernary_WhenTrue_ReturnsActiveStatus()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TernaryConverterProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForTernary { IsActive = true, Name = "Test" };

        // Act
        var dest = mapper.Map<DestForTernary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Active", dest.Status);
    }

    [Fact]
    public void LambdaConverter_WithTernary_WhenFalse_ReturnsInactiveStatus()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TernaryConverterProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForTernary { IsActive = false, Name = "Test" };

        // Act
        var dest = mapper.Map<DestForTernary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Inactive", dest.Status);
    }

    [Fact]
    public void LambdaConverter_WithNullCoalescing_WhenHasValue_UsesValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForNullCoalescing { Value = "actual" };

        // Act
        var dest = mapper.Map<DestForNullCoalescing>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("actual", dest.ValueOrDefault);
    }

    [Fact]
    public void LambdaConverter_WithNullCoalescing_WhenNull_UsesDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForNullCoalescing { Value = null };

        // Act
        var dest = mapper.Map<DestForNullCoalescing>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("default", dest.ValueOrDefault);
    }

    [Fact]
    public void LambdaConverter_WithStringInterpolation_InlinesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<InterpolationProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForInterpolation { First = "John", Last = "Doe", Age = 30 };

        // Act
        var dest = mapper.Map<DestForInterpolation>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John Doe, Age: 30", dest.Description);
    }

    [Fact]
    public void LambdaConverter_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleTransformProfile>());
        var mapper = config.CreateMapper();
        SourceForSimpleTransform? source = null;

        // Act
        var dest = mapper.Map<DestForSimpleTransform?>(source!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void LambdaConverter_Collection_MapsEachElement()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleTransformProfile>());
        var mapper = config.CreateMapper();
        var sources = new List<SourceForSimpleTransform>
        {
            new SourceForSimpleTransform { Value = 1 },
            new SourceForSimpleTransform { Value = 2 },
            new SourceForSimpleTransform { Value = 3 }
        };

        // Act
        var dests = mapper.Map<List<DestForSimpleTransform>>(sources);

        // Assert
        Assert.NotNull(dests);
        Assert.Equal(3, dests.Count);
        Assert.Equal(2, dests[0].DoubleValue);
        Assert.Equal(4, dests[1].DoubleValue);
        Assert.Equal(6, dests[2].DoubleValue);
    }

    #endregion
}
