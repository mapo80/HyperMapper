using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v12.0.0: Tests for Source Generator support of complex LINQ expressions in MapFrom.
/// These tests verify that expressions like .Where().Select() are handled correctly
/// and element types are properly converted.
/// </summary>
public class ComplexLinqExpressionSourceGeneratorTests
{
    #region Test Types

    public class Tag
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class TagDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class PostazioneTag
    {
        public int Id { get; set; }
        public Tag? TagNavigation { get; set; }
    }

    public class Postazione
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public ICollection<PostazioneTag>? Tags { get; set; }
    }

    public class PostazioneDto
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public IEnumerable<TagDto>? Tags { get; set; }
    }

    // For nullable value type tests
    public class SourceWithNullableInt
    {
        public int? Value { get; set; }
        public int? Count { get; set; }
    }

    public class DestWithInt
    {
        public int Value { get; set; }
        public int Total { get; set; }
    }

    #endregion

    #region Test Profiles

    public class LinqSelectWithMappingProfile : Profile
    {
        public LinqSelectWithMappingProfile()
        {
            CreateMap<Tag, TagDto>();
            CreateMap<Postazione, PostazioneDto>()
                .ForMember(dest => dest.Tags,
                    opt => opt.MapFrom(src => (src.Tags ?? Array.Empty<PostazioneTag>())
                        .Where(pt => pt.TagNavigation != null)
                        .Select(pt => pt.TagNavigation!)));
        }
    }

    public class NullableToNonNullableProfile : Profile
    {
        public NullableToNonNullableProfile()
        {
            CreateMap<SourceWithNullableInt, DestWithInt>()
                .ForMember(d => d.Value, o => o.MapFrom(s => s.Value ?? 0))
                .ForMember(d => d.Total, o => o.MapFrom(s => (s.Value ?? 0) + (s.Count ?? 0)));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void LinqSelectWithNavigation_MapsElementsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinqSelectWithMappingProfile>());
        var mapper = config.CreateMapper();

        var source = new Postazione
        {
            Id = 1,
            Nome = "Test",
            Tags = new List<PostazioneTag>
            {
                new() { Id = 1, TagNavigation = new Tag { Id = 10, Name = "Tag1" } },
                new() { Id = 2, TagNavigation = new Tag { Id = 20, Name = "Tag2" } },
                new() { Id = 3, TagNavigation = null } // Should be filtered out
            }
        };

        // Act
        var result = mapper.Map<PostazioneDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Nome);
        Assert.NotNull(result.Tags);

        var tagsList = result.Tags.ToList();
        Assert.Equal(2, tagsList.Count);
        Assert.Equal(10, tagsList[0].Id);
        Assert.Equal("Tag1", tagsList[0].Name);
        Assert.Equal(20, tagsList[1].Id);
        Assert.Equal("Tag2", tagsList[1].Name);
    }

    [Fact]
    public void LinqSelectWithNavigation_HandlesNullCollection()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinqSelectWithMappingProfile>());
        var mapper = config.CreateMapper();

        var source = new Postazione
        {
            Id = 1,
            Nome = "Test",
            Tags = null
        };

        // Act
        var result = mapper.Map<PostazioneDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        // Tags should be empty list, not null (due to Array.Empty fallback)
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void LinqSelectWithNavigation_HandlesEmptyCollection()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinqSelectWithMappingProfile>());
        var mapper = config.CreateMapper();

        var source = new Postazione
        {
            Id = 1,
            Nome = "Test",
            Tags = new List<PostazioneTag>()
        };

        // Act
        var result = mapper.Map<PostazioneDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void NullableToNonNullable_MapsWithCoalescing()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableToNonNullableProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableInt
        {
            Value = 10,
            Count = 5
        };

        // Act
        var result = mapper.Map<DestWithInt>(source);

        // Assert
        Assert.Equal(10, result.Value);
        Assert.Equal(15, result.Total);
    }

    [Fact]
    public void NullableToNonNullable_HandlesNullValues()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableToNonNullableProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableInt
        {
            Value = null,
            Count = null
        };

        // Act
        var result = mapper.Map<DestWithInt>(source);

        // Assert
        Assert.Equal(0, result.Value);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void NullableToNonNullable_MixedNullValues()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableToNonNullableProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableInt
        {
            Value = 7,
            Count = null
        };

        // Act
        var result = mapper.Map<DestWithInt>(source);

        // Assert
        Assert.Equal(7, result.Value);
        Assert.Equal(7, result.Total); // 7 + 0
    }

    #endregion
}
