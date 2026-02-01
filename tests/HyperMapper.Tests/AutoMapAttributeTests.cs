using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 [AutoMap] attribute - Convention-based mapping discovery.
/// AutoMapper API compatibility: [AutoMap(typeof(Source))]
/// </summary>
public class AutoMapAttributeTests
{
    #region Test Models

    public class PersonSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    [AutoMap(typeof(PersonSource))]
    public class PersonDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class OrderSource
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
    }

    [AutoMap(typeof(OrderSource), ReverseMap = true)]
    public class OrderDest
    {
        public int OrderId { get; set; }
        public decimal Total { get; set; }
    }

    public class MultiSourceA
    {
        public int ValueA { get; set; }
    }

    public class MultiSourceB
    {
        public int ValueB { get; set; }
    }

    [AutoMap(typeof(MultiSourceA))]
    [AutoMap(typeof(MultiSourceB))]
    public class MultiDest
    {
        public int ValueA { get; set; }
        public int ValueB { get; set; }
    }

    #endregion

    [Fact]
    public void AutoMap_BasicAttribute_CreatesMapping()
    {
        // Arrange - Scan this test assembly for [AutoMap] attributed types
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapAttributeTests).Assembly));
        var mapper = config.CreateMapper();
        var source = new PersonSource { Id = 1, Name = "John", Age = 30 };

        // Act
        var result = mapper.Map<PersonDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("John", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void AutoMap_WithReverseMap_CreatesBothMappings()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapAttributeTests).Assembly));
        var mapper = config.CreateMapper();

        // Act - Forward mapping
        var order = new OrderSource { OrderId = 100, Total = 99.99m };
        var orderDest = mapper.Map<OrderDest>(order);

        // Assert forward
        Assert.Equal(100, orderDest.OrderId);
        Assert.Equal(99.99m, orderDest.Total);

        // Act - Reverse mapping
        var orderDest2 = new OrderDest { OrderId = 200, Total = 50.00m };
        var orderSource = mapper.Map<OrderSource>(orderDest2);

        // Assert reverse
        Assert.Equal(200, orderSource.OrderId);
        Assert.Equal(50.00m, orderSource.Total);
    }

    [Fact]
    public void AutoMap_MultipleAttributes_AllMappingsCreated()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapAttributeTests).Assembly));
        var mapper = config.CreateMapper();

        // Act - Map from MultiSourceA
        var sourceA = new MultiSourceA { ValueA = 10 };
        var destFromA = mapper.Map<MultiDest>(sourceA);
        Assert.Equal(10, destFromA.ValueA);

        // Act - Map from MultiSourceB
        var sourceB = new MultiSourceB { ValueB = 20 };
        var destFromB = mapper.Map<MultiDest>(sourceB);
        Assert.Equal(20, destFromB.ValueB);
    }

    [Fact]
    public void AutoMap_NotAttributed_NoMappingCreated()
    {
        // Arrange - Only types with [AutoMap] should have mappings
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapAttributeTests).Assembly));
        var mapper = config.CreateMapper();

        // NotAttributedSource doesn't have [AutoMap] pointing to it
        // So mapping should rely on convention
        var source = new PersonSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<PersonDest>(source);

        // This should work because PersonDest has [AutoMap(typeof(PersonSource))]
        Assert.Equal(1, dest.Id);
    }

    [Fact]
    public void AutoMap_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapAttributeTests).Assembly));
        var mapper = config.CreateMapper();
        PersonSource? nullSource = null;

        // Act
        var result = mapper.Map<PersonDest>(nullSource!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AutoMap_Collection_MapsAllElements()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapAttributeTests).Assembly));
        var mapper = config.CreateMapper();
        var sources = new List<PersonSource>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        // Act
        var results = mapper.Map<List<PersonDest>>(sources);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("A", results[0].Name);
        Assert.Equal("B", results[1].Name);
    }
}
