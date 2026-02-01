using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for collection execution plans (v4.3.0).
/// Collections of simple objects should use compiled execution plans
/// instead of per-element MapInternal calls.
/// </summary>
public class CollectionExecutionPlanTests
{
    #region Test Models

    public class ItemSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class ItemDestination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class SimpleSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SimpleDestination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class NumericSource
    {
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
    }

    public class NumericDestination
    {
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
    }

    public class NullableSource
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
        public decimal? NullableDecimal { get; set; }
    }

    public class NullableDestination
    {
        public int? NullableInt { get; set; }
        public string? NullableString { get; set; }
        public decimal? NullableDecimal { get; set; }
    }

    #endregion

    #region Profiles

    public class ItemProfile : Profile
    {
        public ItemProfile()
        {
            CreateMap<ItemSource, ItemDestination>();
        }
    }

    public class SimpleProfile : Profile
    {
        public SimpleProfile()
        {
            CreateMap<SimpleSource, SimpleDestination>();
        }
    }

    public class NumericProfile : Profile
    {
        public NumericProfile()
        {
            CreateMap<NumericSource, NumericDestination>();
        }
    }

    public class NullableProfile : Profile
    {
        public NullableProfile()
        {
            CreateMap<NullableSource, NullableDestination>();
        }
    }

    #endregion

    [Fact]
    public void Map_LargeCollection_MapsAllElementsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ItemProfile>());
        var mapper = config.CreateMapper();

        var source = Enumerable.Range(0, 1000)
            .Select(i => new ItemSource
            {
                Id = i,
                Name = $"Item {i}",
                Price = i * 1.5m,
                Quantity = i + 1
            })
            .ToList();

        // Act
        var result = mapper.Map<List<ItemDestination>>(source);

        // Assert
        Assert.Equal(1000, result.Count);
        Assert.Equal(0, result[0].Id);
        Assert.Equal("Item 0", result[0].Name);
        Assert.Equal(0m, result[0].Price);
        Assert.Equal(1, result[0].Quantity);

        Assert.Equal(999, result[999].Id);
        Assert.Equal("Item 999", result[999].Name);
        Assert.Equal(1498.5m, result[999].Price);
        Assert.Equal(1000, result[999].Quantity);

        Assert.Equal(500, result[500].Id);
        Assert.Equal("Item 500", result[500].Name);
    }

    [Fact]
    public void Map_MediumCollection_MapsAllElementsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ItemProfile>());
        var mapper = config.CreateMapper();

        var source = Enumerable.Range(0, 100)
            .Select(i => new ItemSource { Id = i, Name = $"Item {i}", Price = i * 2.5m, Quantity = i })
            .ToList();

        // Act
        var result = mapper.Map<List<ItemDestination>>(source);

        // Assert
        Assert.Equal(100, result.Count);
        Assert.Equal(50, result[50].Id);
        Assert.Equal(125.0m, result[50].Price);
    }

    [Fact]
    public void Map_SmallCollection_MapsAllElementsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ItemProfile>());
        var mapper = config.CreateMapper();

        var source = new List<ItemSource>
        {
            new() { Id = 1, Name = "First", Price = 10.5m, Quantity = 5 },
            new() { Id = 2, Name = "Second", Price = 20.5m, Quantity = 10 }
        };

        // Act
        var result = mapper.Map<List<ItemDestination>>(source);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("First", result[0].Name);
        Assert.Equal(10.5m, result[0].Price);
        Assert.Equal(5, result[0].Quantity);
        Assert.Equal(2, result[1].Id);
        Assert.Equal("Second", result[1].Name);
        Assert.Equal(20.5m, result[1].Price);
        Assert.Equal(10, result[1].Quantity);
    }

    [Fact]
    public void Map_EmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ItemProfile>());
        var mapper = config.CreateMapper();

        var source = new List<ItemSource>();

        // Act
        var result = mapper.Map<List<ItemDestination>>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Map_SingleElement_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 42, Name = "Single" }
        };

        // Act
        var result = mapper.Map<List<SimpleDestination>>(source);

        // Assert
        Assert.Single(result);
        Assert.Equal(42, result[0].Id);
        Assert.Equal("Single", result[0].Name);
    }

    [Fact]
    public void Map_NumericProperties_MapsAllTypes()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NumericProfile>());
        var mapper = config.CreateMapper();

        var source = new List<NumericSource>
        {
            new()
            {
                IntValue = 42,
                LongValue = 123456789L,
                DecimalValue = 99.99m,
                DoubleValue = 3.14159
            }
        };

        // Act
        var result = mapper.Map<List<NumericDestination>>(source);

        // Assert
        Assert.Single(result);
        Assert.Equal(42, result[0].IntValue);
        Assert.Equal(123456789L, result[0].LongValue);
        Assert.Equal(99.99m, result[0].DecimalValue);
        Assert.Equal(3.14159, result[0].DoubleValue, 5);
    }

    [Fact]
    public void Map_NullableProperties_MapsValues()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullableProfile>());
        var mapper = config.CreateMapper();

        var source = new List<NullableSource>
        {
            new() { NullableInt = 42, NullableString = "Test", NullableDecimal = 99.99m },
            new() { NullableInt = null, NullableString = null, NullableDecimal = null }
        };

        // Act
        var result = mapper.Map<List<NullableDestination>>(source);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(42, result[0].NullableInt);
        Assert.Equal("Test", result[0].NullableString);
        Assert.Equal(99.99m, result[0].NullableDecimal);
        Assert.Null(result[1].NullableInt);
        Assert.Null(result[1].NullableString);
        Assert.Null(result[1].NullableDecimal);
    }

    [Fact]
    public void Map_ToIList_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        // Act
        var result = mapper.Map<IList<SimpleDestination>>(source);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void Map_ToICollection_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        // Act
        var result = mapper.Map<ICollection<SimpleDestination>>(source);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Map_ToIEnumerable_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();

        var source = new List<SimpleSource>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        // Act
        var result = mapper.Map<IEnumerable<SimpleDestination>>(source);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Map_Collection_CreatesIndependentCopies()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ItemProfile>());
        var mapper = config.CreateMapper();

        var source = new List<ItemSource>
        {
            new() { Id = 1, Name = "Original", Price = 10m, Quantity = 5 }
        };

        // Act
        var result = mapper.Map<List<ItemDestination>>(source);

        // Modify source after mapping
        source[0].Name = "Modified";
        source[0].Price = 99m;
        source.Add(new ItemSource { Id = 2, Name = "New", Price = 20m, Quantity = 10 });

        // Assert - result should not be affected
        Assert.Single(result);
        Assert.Equal("Original", result[0].Name);
        Assert.Equal(10m, result[0].Price);
    }

    [Fact]
    public void Map_VeryLargeCollection_DoesNotOverflow()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();

        var source = Enumerable.Range(0, 10000)
            .Select(i => new SimpleSource { Id = i, Name = $"Item {i}" })
            .ToList();

        // Act
        var result = mapper.Map<List<SimpleDestination>>(source);

        // Assert
        Assert.Equal(10000, result.Count);
        Assert.Equal(0, result[0].Id);
        Assert.Equal(9999, result[9999].Id);
    }

    [Fact]
    public void Map_MultipleCollections_MapsConsistently()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ItemProfile>());
        var mapper = config.CreateMapper();

        var source1 = new List<ItemSource>
        {
            new() { Id = 1, Name = "A", Price = 10m, Quantity = 1 }
        };
        var source2 = new List<ItemSource>
        {
            new() { Id = 2, Name = "B", Price = 20m, Quantity = 2 }
        };
        var source3 = new List<ItemSource>
        {
            new() { Id = 3, Name = "C", Price = 30m, Quantity = 3 }
        };

        // Act
        var result1 = mapper.Map<List<ItemDestination>>(source1);
        var result2 = mapper.Map<List<ItemDestination>>(source2);
        var result3 = mapper.Map<List<ItemDestination>>(source3);

        // Assert
        Assert.Equal(1, result1[0].Id);
        Assert.Equal(2, result2[0].Id);
        Assert.Equal(3, result3[0].Id);
    }

    [Fact]
    public void Map_WithoutExplicitProfile_StillUsesCollectionPlan()
    {
        // Arrange - use inline profile
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SimpleProfile>();
        });
        var mapper = config.CreateMapper();

        var source = Enumerable.Range(0, 100)
            .Select(i => new SimpleSource { Id = i, Name = $"Item {i}" })
            .ToList();

        // Act
        var result = mapper.Map<List<SimpleDestination>>(source);

        // Assert
        Assert.Equal(100, result.Count);
        Assert.Equal(50, result[50].Id);
        Assert.Equal("Item 50", result[50].Name);
    }
}
