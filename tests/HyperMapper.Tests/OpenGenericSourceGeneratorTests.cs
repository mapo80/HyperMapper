using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for Source Generator support of open generic types.
/// These tests verify that open generic mappings (e.g., Box&lt;T&gt; to BoxDto&lt;T&gt;) work correctly.
/// Note: Open generics are handled at runtime but the Source Generator produces generic methods.
/// </summary>
public class OpenGenericSourceGeneratorTests
{
    #region Test Types

    public class Box<T>
    {
        public T? Value { get; set; }
    }

    public class BoxDto<T>
    {
        public T? Value { get; set; }
    }

    public class Pair<TKey, TValue>
    {
        public TKey? Key { get; set; }
        public TValue? Value { get; set; }
    }

    public class PairDto<TKey, TValue>
    {
        public TKey? Key { get; set; }
        public TValue? Value { get; set; }
    }

    public class Container<T>
    {
        public T? Item { get; set; }
        public string? Name { get; set; }
    }

    public class ContainerDto<T>
    {
        public T? Item { get; set; }
        public string? Name { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    #endregion

    #region Test Profiles

    public class SimpleBoxProfile : Profile
    {
        public SimpleBoxProfile()
        {
            CreateMap(typeof(Box<>), typeof(BoxDto<>));
        }
    }

    public class TwoTypeParamsProfile : Profile
    {
        public TwoTypeParamsProfile()
        {
            CreateMap(typeof(Pair<,>), typeof(PairDto<,>));
        }
    }

    public class ContainerWithStringProfile : Profile
    {
        public ContainerWithStringProfile()
        {
            CreateMap(typeof(Container<>), typeof(ContainerDto<>));
        }
    }

    public class BoxWithNestedMappingProfile : Profile
    {
        public BoxWithNestedMappingProfile()
        {
            CreateMap<Customer, CustomerDto>();
            CreateMap(typeof(Box<>), typeof(BoxDto<>));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void OpenGeneric_SimpleBox_WithValueType_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoxProfile>());
        var mapper = config.CreateMapper();
        var source = new Box<int> { Value = 42 };

        // Act
        var dest = mapper.Map<BoxDto<int>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void OpenGeneric_SimpleBox_WithReferenceType_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoxProfile>());
        var mapper = config.CreateMapper();
        var source = new Box<string> { Value = "hello" };

        // Act
        var dest = mapper.Map<BoxDto<string>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("hello", dest.Value);
    }

    [Fact]
    public void OpenGeneric_TwoTypeParams_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TwoTypeParamsProfile>());
        var mapper = config.CreateMapper();
        var source = new Pair<string, int> { Key = "answer", Value = 42 };

        // Act
        var dest = mapper.Map<PairDto<string, int>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("answer", dest.Key);
        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void OpenGeneric_ContainerWithString_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ContainerWithStringProfile>());
        var mapper = config.CreateMapper();
        var source = new Container<int>
        {
            Item = 100,
            Name = "Test Container"
        };

        // Act
        var dest = mapper.Map<ContainerDto<int>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(100, dest.Item);
        Assert.Equal("Test Container", dest.Name);
    }

    [Fact]
    public void OpenGeneric_WithNullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoxProfile>());
        var mapper = config.CreateMapper();
        Box<int>? source = null;

        // Act
        var dest = mapper.Map<BoxDto<int>?>(source!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void OpenGeneric_WithNullValue_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoxProfile>());
        var mapper = config.CreateMapper();
        var source = new Box<string> { Value = null };

        // Act
        var dest = mapper.Map<BoxDto<string>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Null(dest.Value);
    }

    [Fact]
    public void OpenGeneric_Collection_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleBoxProfile>());
        var mapper = config.CreateMapper();
        var sources = new List<Box<int>>
        {
            new Box<int> { Value = 1 },
            new Box<int> { Value = 2 },
            new Box<int> { Value = 3 }
        };

        // Act
        var dests = mapper.Map<List<BoxDto<int>>>(sources);

        // Assert
        Assert.NotNull(dests);
        Assert.Equal(3, dests.Count);
        Assert.Equal(1, dests[0].Value);
        Assert.Equal(2, dests[1].Value);
        Assert.Equal(3, dests[2].Value);
    }

    [Fact]
    public void OpenGeneric_WithNestedComplexType_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BoxWithNestedMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new Box<Customer>
        {
            Value = new Customer { Id = 1, Name = "John Doe" }
        };

        // Act
        var dest = mapper.Map<BoxDto<CustomerDto>>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Value);
        Assert.Equal(1, dest.Value.Id);
        Assert.Equal("John Doe", dest.Value.Name);
    }

    #endregion
}
