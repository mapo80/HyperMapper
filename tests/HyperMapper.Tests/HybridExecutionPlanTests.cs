using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for hybrid execution plan functionality.
/// Hybrid execution plans compile non-collection properties into an execution plan
/// while delegating collection properties to the legacy mapping path.
/// </summary>
public class HybridExecutionPlanTests
{
    #region Test Models

    public class SourceWithCollection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class DestinationWithCollection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class AddressSource
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class AddressDestination
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class SourceWithCollectionAndNested
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public AddressSource? Address { get; set; }
    }

    public class DestinationWithCollectionAndNested
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public AddressDestination? Address { get; set; }
    }

    public class SourceWithMultipleCollections
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
    }

    public class DestinationWithMultipleCollections
    {
        public int Id { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
    }

    public class SourceWithDictionary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, int> Metadata { get; set; } = new();
    }

    public class DestinationWithDictionary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, int> Metadata { get; set; } = new();
    }

    public class OnlyCollectionsSource
    {
        public List<string> Tags { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
    }

    public class OnlyCollectionsDestination
    {
        public List<string> Tags { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
    }

    #endregion

    #region Profiles

    public class CollectionProfile : Profile
    {
        public CollectionProfile()
        {
            CreateMap<SourceWithCollection, DestinationWithCollection>();
        }
    }

    public class CollectionAndNestedProfile : Profile
    {
        public CollectionAndNestedProfile()
        {
            CreateMap<SourceWithCollectionAndNested, DestinationWithCollectionAndNested>();
            CreateMap<AddressSource, AddressDestination>();
        }
    }

    public class MultipleCollectionsProfile : Profile
    {
        public MultipleCollectionsProfile()
        {
            CreateMap<SourceWithMultipleCollections, DestinationWithMultipleCollections>();
        }
    }

    public class DictionaryProfile : Profile
    {
        public DictionaryProfile()
        {
            CreateMap<SourceWithDictionary, DestinationWithDictionary>();
        }
    }

    public class OnlyCollectionsProfile : Profile
    {
        public OnlyCollectionsProfile()
        {
            CreateMap<OnlyCollectionsSource, OnlyCollectionsDestination>();
        }
    }

    #endregion

    [Fact]
    public void Map_ObjectWithSimplePropertiesAndCollection_MapsAllProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCollection
        {
            Id = 42,
            Name = "Test",
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        var result = mapper.Map<DestinationWithCollection>(source);

        // Assert
        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal(3, result.Tags.Count);
        Assert.Contains("tag1", result.Tags);
        Assert.Contains("tag2", result.Tags);
        Assert.Contains("tag3", result.Tags);
    }

    [Fact]
    public void Map_ObjectWithCollectionAndNestedObject_MapsAllProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionAndNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCollectionAndNested
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string> { "tag1", "tag2" },
            Address = new AddressSource { Street = "Via Roma 1", City = "Milano" }
        };

        // Act
        var result = mapper.Map<DestinationWithCollectionAndNested>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("tag1", result.Tags);
        Assert.Contains("tag2", result.Tags);
        Assert.NotNull(result.Address);
        Assert.Equal("Via Roma 1", result.Address!.Street);
        Assert.Equal("Milano", result.Address.City);
    }

    [Fact]
    public void Map_ObjectWithNullCollection_CreatesEmptyCollection()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCollection
        {
            Id = 1,
            Name = "Test",
            Tags = null!
        };

        // Act
        var result = mapper.Map<DestinationWithCollection>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void Map_ObjectWithEmptyCollection_MapsEmptyCollection()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCollection
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string>()
        };

        // Act
        var result = mapper.Map<DestinationWithCollection>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.NotNull(result.Tags);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void Map_ObjectWithMultipleCollections_MapsAllCollections()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleCollectionsProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithMultipleCollections
        {
            Id = 1,
            Tags = new List<string> { "a", "b" },
            Numbers = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = mapper.Map<DestinationWithMultipleCollections>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.Tags.Count);
        Assert.Equal(3, result.Numbers.Count);
        Assert.Contains("a", result.Tags);
        Assert.Contains(3, result.Numbers);
    }

    [Fact]
    public void Map_ObjectWithDictionary_MapsDictionary()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictionaryProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDictionary
        {
            Id = 1,
            Name = "Test",
            Metadata = new Dictionary<string, int>
            {
                { "key1", 100 },
                { "key2", 200 }
            }
        };

        // Act
        var result = mapper.Map<DestinationWithDictionary>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Equal(2, result.Metadata.Count);
        Assert.Equal(100, result.Metadata["key1"]);
        Assert.Equal(200, result.Metadata["key2"]);
    }

    [Fact]
    public void Map_ObjectWithNullDictionary_CreatesEmptyDictionary()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictionaryProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDictionary
        {
            Id = 1,
            Name = "Test",
            Metadata = null!
        };

        // Act
        var result = mapper.Map<DestinationWithDictionary>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.NotNull(result.Metadata);
        Assert.Empty(result.Metadata);
    }

    [Fact]
    public void Map_ObjectWithOnlyCollections_MapsAllCollections()
    {
        // This case should fall back to legacy path since there are no non-collection properties
        // to compile into an execution plan

        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OnlyCollectionsProfile>());
        var mapper = config.CreateMapper();

        var source = new OnlyCollectionsSource
        {
            Tags = new List<string> { "x", "y" },
            Numbers = new List<int> { 5, 6, 7 }
        };

        // Act
        var result = mapper.Map<OnlyCollectionsDestination>(source);

        // Assert
        Assert.Equal(2, result.Tags.Count);
        Assert.Equal(3, result.Numbers.Count);
    }

    [Fact]
    public void Map_ObjectWithCollectionAndNullNestedObject_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionAndNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCollectionAndNested
        {
            Id = 1,
            Name = "Test",
            Tags = new List<string> { "tag1" },
            Address = null
        };

        // Act
        var result = mapper.Map<DestinationWithCollectionAndNested>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.Single(result.Tags);
        Assert.Null(result.Address);
    }

    [Fact]
    public void Map_MultipleObjects_MapsConsistently()
    {
        // Ensure hybrid execution works consistently across multiple mapping calls

        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        var mapper = config.CreateMapper();

        var sources = new[]
        {
            new SourceWithCollection { Id = 1, Name = "First", Tags = new List<string> { "a" } },
            new SourceWithCollection { Id = 2, Name = "Second", Tags = new List<string> { "b", "c" } },
            new SourceWithCollection { Id = 3, Name = "Third", Tags = new List<string>() }
        };

        // Act
        var results = sources.Select(s => mapper.Map<DestinationWithCollection>(s)).ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("First", results[0].Name);
        Assert.Single(results[0].Tags);

        Assert.Equal(2, results[1].Id);
        Assert.Equal("Second", results[1].Name);
        Assert.Equal(2, results[1].Tags.Count);

        Assert.Equal(3, results[2].Id);
        Assert.Equal("Third", results[2].Name);
        Assert.Empty(results[2].Tags);
    }
}
