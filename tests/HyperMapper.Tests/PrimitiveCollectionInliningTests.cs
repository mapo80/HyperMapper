using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for primitive collection inlining optimization (v4.2.0).
/// Primitive collections (List&lt;string&gt;, List&lt;int&gt;, etc.) should be inlined
/// directly into the execution plan instead of using the legacy path.
/// </summary>
public class PrimitiveCollectionInliningTests
{
    #region Test Models

    public class SourceWithStringList
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class DestinationWithStringList
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class SourceWithIntList
    {
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = new();
    }

    public class DestinationWithIntList
    {
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = new();
    }

    public class SourceWithMultiplePrimitiveLists
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
        public List<decimal> Prices { get; set; } = new();
    }

    public class DestinationWithMultiplePrimitiveLists
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
        public List<decimal> Prices { get; set; } = new();
    }

    public class NestedSource
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class NestedDestination
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class SourceWithListAndNested
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; } = new();
        public NestedSource? Address { get; set; }
    }

    public class DestinationWithListAndNested
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; } = new();
        public NestedDestination? Address { get; set; }
    }

    public class SourceWithGuidList
    {
        public List<Guid> Ids { get; set; } = new();
    }

    public class DestinationWithGuidList
    {
        public List<Guid> Ids { get; set; } = new();
    }

    public class SourceWithDateTimeList
    {
        public List<DateTime> Dates { get; set; } = new();
    }

    public class DestinationWithDateTimeList
    {
        public List<DateTime> Dates { get; set; } = new();
    }

    #endregion

    #region Profiles

    public class StringListProfile : Profile
    {
        public StringListProfile()
        {
            CreateMap<SourceWithStringList, DestinationWithStringList>();
        }
    }

    public class IntListProfile : Profile
    {
        public IntListProfile()
        {
            CreateMap<SourceWithIntList, DestinationWithIntList>();
        }
    }

    public class MultiplePrimitiveListsProfile : Profile
    {
        public MultiplePrimitiveListsProfile()
        {
            CreateMap<SourceWithMultiplePrimitiveLists, DestinationWithMultiplePrimitiveLists>();
        }
    }

    public class ListAndNestedProfile : Profile
    {
        public ListAndNestedProfile()
        {
            CreateMap<SourceWithListAndNested, DestinationWithListAndNested>();
            CreateMap<NestedSource, NestedDestination>();
        }
    }

    public class GuidListProfile : Profile
    {
        public GuidListProfile()
        {
            CreateMap<SourceWithGuidList, DestinationWithGuidList>();
        }
    }

    public class DateTimeListProfile : Profile
    {
        public DateTimeListProfile()
        {
            CreateMap<SourceWithDateTimeList, DestinationWithDateTimeList>();
        }
    }

    #endregion

    [Fact]
    public void Map_ListOfStrings_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringListProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringList
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        var result = mapper.Map<DestinationWithStringList>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal(3, result.Tags.Count);
        Assert.Contains("tag1", result.Tags);
        Assert.Contains("tag2", result.Tags);
        Assert.Contains("tag3", result.Tags);
    }

    [Fact]
    public void Map_ListOfInts_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IntListProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithIntList
        {
            Id = 42,
            Numbers = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = mapper.Map<DestinationWithIntList>(source);

        // Assert
        Assert.Equal(42, result.Id);
        Assert.Equal(5, result.Numbers.Count);
        Assert.Contains(1, result.Numbers);
        Assert.Contains(5, result.Numbers);
    }

    [Fact]
    public void Map_NullStringList_CreatesEmptyList()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringListProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringList
        {
            Id = 1,
            Name = "Test",
            Tags = null!
        };

        // Act
        var result = mapper.Map<DestinationWithStringList>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void Map_EmptyStringList_MapsEmptyList()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringListProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringList
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string>()
        };

        // Act
        var result = mapper.Map<DestinationWithStringList>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void Map_MultiplePrimitiveLists_MapsAllCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultiplePrimitiveListsProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithMultiplePrimitiveLists
        {
            Id = 1,
            Tags = new List<string> { "a", "b" },
            Numbers = new List<int> { 10, 20, 30 },
            Prices = new List<decimal> { 9.99m, 19.99m }
        };

        // Act
        var result = mapper.Map<DestinationWithMultiplePrimitiveLists>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.Tags.Count);
        Assert.Equal(3, result.Numbers.Count);
        Assert.Equal(2, result.Prices.Count);
        Assert.Contains("a", result.Tags);
        Assert.Contains(20, result.Numbers);
        Assert.Contains(19.99m, result.Prices);
    }

    [Fact]
    public void Map_ListAndNestedObject_MapsAllCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ListAndNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithListAndNested
        {
            Id = 1,
            Tags = new List<string> { "tag1", "tag2" },
            Address = new NestedSource { Street = "Via Roma 1", City = "Milano" }
        };

        // Act
        var result = mapper.Map<DestinationWithListAndNested>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("tag1", result.Tags);
        Assert.NotNull(result.Address);
        Assert.Equal("Via Roma 1", result.Address!.Street);
        Assert.Equal("Milano", result.Address.City);
    }

    [Fact]
    public void Map_ListWithNullNested_MapsListCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ListAndNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithListAndNested
        {
            Id = 1,
            Tags = new List<string> { "tag1" },
            Address = null
        };

        // Act
        var result = mapper.Map<DestinationWithListAndNested>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Single(result.Tags);
        Assert.Null(result.Address);
    }

    [Fact]
    public void Map_ListOfGuids_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<GuidListProfile>());
        var mapper = config.CreateMapper();

        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        var source = new SourceWithGuidList
        {
            Ids = new List<Guid> { guid1, guid2 }
        };

        // Act
        var result = mapper.Map<DestinationWithGuidList>(source);

        // Assert
        Assert.Equal(2, result.Ids.Count);
        Assert.Contains(guid1, result.Ids);
        Assert.Contains(guid2, result.Ids);
    }

    [Fact]
    public void Map_ListOfDateTimes_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DateTimeListProfile>());
        var mapper = config.CreateMapper();

        var date1 = new DateTime(2025, 1, 1);
        var date2 = new DateTime(2025, 12, 31);

        var source = new SourceWithDateTimeList
        {
            Dates = new List<DateTime> { date1, date2 }
        };

        // Act
        var result = mapper.Map<DestinationWithDateTimeList>(source);

        // Assert
        Assert.Equal(2, result.Dates.Count);
        Assert.Contains(date1, result.Dates);
        Assert.Contains(date2, result.Dates);
    }

    [Fact]
    public void Map_ListOfStrings_CreatesIndependentCopy()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringListProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithStringList
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var result = mapper.Map<DestinationWithStringList>(source);

        // Modify source after mapping
        source.Tags.Add("tag3");
        source.Tags[0] = "modified";

        // Assert - result should not be affected
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("tag1", result.Tags);
        Assert.Contains("tag2", result.Tags);
        Assert.DoesNotContain("tag3", result.Tags);
    }

    [Fact]
    public void Map_MultipleObjects_MapsConsistently()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringListProfile>());
        var mapper = config.CreateMapper();

        var sources = new[]
        {
            new SourceWithStringList { Id = 1, Name = "First", Tags = new List<string> { "a" } },
            new SourceWithStringList { Id = 2, Name = "Second", Tags = new List<string> { "b", "c" } },
            new SourceWithStringList { Id = 3, Name = "Third", Tags = new List<string>() }
        };

        // Act
        var results = sources.Select(s => mapper.Map<DestinationWithStringList>(s)).ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Single(results[0].Tags);
        Assert.Equal(2, results[1].Tags.Count);
        Assert.Empty(results[2].Tags);
    }
}
