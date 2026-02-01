using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 AddTransform<T>() feature - value transformations.
/// AutoMapper API compatibility: CreateMap<Source, Dest>().AddTransform<string>(s => s.Trim())
/// </summary>
public class AddTransformTests
{
    #region Test Models

    public class TransformSource
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Value { get; set; }
        public decimal Amount { get; set; }
    }

    public class TransformDestination
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Value { get; set; }
        public decimal Amount { get; set; }
    }

    public class NestedSource
    {
        public int Id { get; set; }
        public InnerSource? Inner { get; set; }
    }

    public class InnerSource
    {
        public string? Text { get; set; }
    }

    public class NestedDestination
    {
        public int Id { get; set; }
        public InnerDestination? Inner { get; set; }
    }

    public class InnerDestination
    {
        public string? Text { get; set; }
    }

    #endregion

    #region Profiles

    public class StringTrimTransformProfile : Profile
    {
        public StringTrimTransformProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<string>(s => s.Trim());
        }
    }

    public class StringUpperTransformProfile : Profile
    {
        public StringUpperTransformProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<string>(s => s.ToUpper());
        }
    }

    public class IntTransformProfile : Profile
    {
        public IntTransformProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<int>(i => i * 2);
        }
    }

    public class MultipleTransformsProfile : Profile
    {
        public MultipleTransformsProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<string>(s => s.Trim())
                .AddTransform<int>(i => i + 10);
        }
    }

    public class ChainedTransformsProfile : Profile
    {
        public ChainedTransformsProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<string>(s => s.Trim())
                .AddTransform<string>(s => s.ToUpper());  // This should override previous
        }
    }

    public class DecimalTransformProfile : Profile
    {
        public DecimalTransformProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<decimal>(d => Math.Round(d, 2));
        }
    }

    public class NestedTransformProfile : Profile
    {
        public NestedTransformProfile()
        {
            CreateMap<InnerSource, InnerDestination>()
                .AddTransform<string>(s => s.ToLower());

            CreateMap<NestedSource, NestedDestination>();
        }
    }

    public class TransformWithNullProfile : Profile
    {
        public TransformWithNullProfile()
        {
            CreateMap<TransformSource, TransformDestination>()
                .AddTransform<string>(s => s ?? "DEFAULT");
        }
    }

    #endregion

    [Fact]
    public void AddTransform_StringTrim_AppliesTransformation()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringTrimTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Name = "  Hello  ", Description = "  World  " };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert
        Assert.Equal("Hello", result.Name);
        Assert.Equal("World", result.Description);
    }

    [Fact]
    public void AddTransform_StringUpper_AppliesTransformation()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringUpperTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Name = "hello", Description = "world" };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert
        Assert.Equal("HELLO", result.Name);
        Assert.Equal("WORLD", result.Description);
    }

    [Fact]
    public void AddTransform_MultipleTypes_AppliesCorrectTransform()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleTransformsProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Name = "  hello  ", Value = 5 };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert
        Assert.Equal("hello", result.Name);
        Assert.Equal(15, result.Value);  // 5 + 10
    }

    [Fact]
    public void AddTransform_WithNullValue_HandlesGracefully()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TransformWithNullProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Name = null };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert - null becomes "DEFAULT"
        Assert.Equal("DEFAULT", result.Name);
    }

    [Fact]
    public void AddTransform_ChainedTransforms_LastWins()
    {
        // Arrange - Second AddTransform<string> should override the first
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ChainedTransformsProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Name = "  hello  " };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert - Only ToUpper is applied (trim is overridden)
        // Note: The last AddTransform<T> wins
        Assert.Equal("  HELLO  ", result.Name);
    }

    [Fact]
    public void AddTransform_IntDouble_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IntTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Value = 21 };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void AddTransform_Decimal_RoundsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DecimalTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new TransformSource { Amount = 123.456789m };

        // Act
        var result = mapper.Map<TransformDestination>(source);

        // Assert
        Assert.Equal(123.46m, result.Amount);
    }

    [Fact]
    public void AddTransform_OnNestedObject_AppliesTransform()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new NestedSource
        {
            Id = 1,
            Inner = new InnerSource { Text = "HELLO WORLD" }
        };

        // Act
        var result = mapper.Map<NestedDestination>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Inner);
        Assert.Equal("hello world", result.Inner!.Text);
    }
}
