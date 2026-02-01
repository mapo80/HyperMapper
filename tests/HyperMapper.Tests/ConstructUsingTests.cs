using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 ConstructUsing() feature - Custom constructor logic.
/// AutoMapper API compatibility: CreateMap<S, D>().ConstructUsing(src => new D(src.Id))
/// </summary>
public class ConstructUsingTests
{
    #region Test Models

    public class OrderSource
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderDestination
    {
        // Required constructor parameter
        public int OrderId { get; }
        public string? CustomerName { get; set; }
        public decimal Total { get; set; }

        public OrderDestination(int orderId)
        {
            OrderId = orderId;
        }
    }

    public class ImmutableDestination
    {
        public int Id { get; }
        public string Name { get; }

        public ImmutableDestination(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class SourceForImmutable
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class FactoryCreatable
    {
        public int Value { get; }
        public string? Tag { get; set; }

        private FactoryCreatable(int value)
        {
            Value = value;
        }

        public static FactoryCreatable Create(int value)
        {
            return new FactoryCreatable(value);
        }
    }

    public class FactorySource
    {
        public int Value { get; set; }
        public string? Tag { get; set; }
    }

    public class ContextAwareDestination
    {
        public int Id { get; set; }
        public string? MappedBy { get; set; }
    }

    public class SimpleSource
    {
        public int Id { get; set; }
    }

    public class WithDependency
    {
        public int Id { get; set; }
        public IService Service { get; }

        public WithDependency(IService service)
        {
            Service = service;
        }
    }

    public interface IService
    {
        string GetValue();
    }

    public class MockService : IService
    {
        public string GetValue() => "Injected";
    }

    #endregion

    #region Profiles

    public class BasicConstructUsingProfile : Profile
    {
        public BasicConstructUsingProfile()
        {
            CreateMap<OrderSource, OrderDestination>()
                .ConstructUsing(src => new OrderDestination(src.Id));
        }
    }

    public class ImmutableConstructUsingProfile : Profile
    {
        public ImmutableConstructUsingProfile()
        {
            CreateMap<SourceForImmutable, ImmutableDestination>()
                .ConstructUsing(src => new ImmutableDestination(src.Id, src.Name ?? ""));
        }
    }

    public class FactoryMethodProfile : Profile
    {
        public FactoryMethodProfile()
        {
            CreateMap<FactorySource, FactoryCreatable>()
                .ConstructUsing(src => FactoryCreatable.Create(src.Value));
        }
    }

    public class ConstructUsingWithContextProfile : Profile
    {
        public ConstructUsingWithContextProfile()
        {
            CreateMap<SimpleSource, ContextAwareDestination>()
                .ConstructUsing((src, ctx) =>
                {
                    return new ContextAwareDestination
                    {
                        Id = src.Id,
                        MappedBy = "ContextConstructor"
                    };
                });
        }
    }

    public class ConstructUsingWithForMemberProfile : Profile
    {
        public ConstructUsingWithForMemberProfile()
        {
            CreateMap<OrderSource, OrderDestination>()
                .ConstructUsing(src => new OrderDestination(src.Id * 10))
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.CustomerName!.ToUpper()));
        }
    }

    public class DependencyInjectionProfile : Profile
    {
        private readonly IService _service;

        public DependencyInjectionProfile(IService service)
        {
            _service = service;
            CreateMap<SimpleSource, WithDependency>()
                .ConstructUsing(src => new WithDependency(_service));
        }
    }

    #endregion

    [Fact]
    public void ConstructUsing_BasicCustomConstructor_UsesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicConstructUsingProfile>());
        var mapper = config.CreateMapper();
        var source = new OrderSource
        {
            Id = 42,
            CustomerName = "John Doe",
            Total = 100.50m
        };

        // Act
        var result = mapper.Map<OrderDestination>(source);

        // Assert
        Assert.Equal(42, result.OrderId); // From constructor
        Assert.Equal("John Doe", result.CustomerName); // From convention mapping
        Assert.Equal(100.50m, result.Total); // From convention mapping
    }

    [Fact]
    public void ConstructUsing_ImmutableType_AllPropertiesSet()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ImmutableConstructUsingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForImmutable { Id = 1, Name = "Test" };

        // Act
        var result = mapper.Map<ImmutableDestination>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void ConstructUsing_FactoryMethod_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FactoryMethodProfile>());
        var mapper = config.CreateMapper();
        var source = new FactorySource { Value = 99, Tag = "MyTag" };

        // Act
        var result = mapper.Map<FactoryCreatable>(source);

        // Assert
        Assert.Equal(99, result.Value); // From factory
        Assert.Equal("MyTag", result.Tag); // From convention mapping
    }

    [Fact]
    public void ConstructUsing_WithResolutionContext_HasAccess()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingWithContextProfile>());
        var mapper = config.CreateMapper();
        var source = new SimpleSource { Id = 5 };

        // Act
        var result = mapper.Map<ContextAwareDestination>(source);

        // Assert
        Assert.Equal(5, result.Id);
        Assert.Equal("ContextConstructor", result.MappedBy);
    }

    [Fact]
    public void ConstructUsing_CombinedWithForMember_BothWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingWithForMemberProfile>());
        var mapper = config.CreateMapper();
        var source = new OrderSource
        {
            Id = 5,
            CustomerName = "jane",
            Total = 50m
        };

        // Act
        var result = mapper.Map<OrderDestination>(source);

        // Assert
        Assert.Equal(50, result.OrderId); // From constructor: 5 * 10
        Assert.Equal("JANE", result.CustomerName); // From ForMember: ToUpper()
        Assert.Equal(50m, result.Total); // From convention mapping
    }

    [Fact]
    public void ConstructUsing_WithDependencyInjection_InjectsCorrectly()
    {
        // Arrange - Simulate DI by passing service to profile
        var service = new MockService();
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new DependencyInjectionProfile(service)));
        var mapper = config.CreateMapper();
        var source = new SimpleSource { Id = 1 };

        // Act
        var result = mapper.Map<WithDependency>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Same(service, result.Service);
        Assert.Equal("Injected", result.Service.GetValue());
    }

    [Fact]
    public void ConstructUsing_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicConstructUsingProfile>());
        var mapper = config.CreateMapper();
        OrderSource? nullSource = null;

        // Act
        var result = mapper.Map<OrderDestination>(nullSource!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConstructUsing_Collection_EachElementUsesConstructor()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicConstructUsingProfile>());
        var mapper = config.CreateMapper();
        var sources = new List<OrderSource>
        {
            new() { Id = 1, CustomerName = "A" },
            new() { Id = 2, CustomerName = "B" },
            new() { Id = 3, CustomerName = "C" }
        };

        // Act
        var results = mapper.Map<List<OrderDestination>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].OrderId);
        Assert.Equal(2, results[1].OrderId);
        Assert.Equal(3, results[2].OrderId);
    }

    [Fact]
    public void ConstructUsing_MapToExisting_IgnoresConstructUsing()
    {
        // Arrange - MapToExisting should NOT use ConstructUsing because we already have an instance
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicConstructUsingProfile>());
        var mapper = config.CreateMapper();
        var source = new OrderSource { Id = 99, CustomerName = "New", Total = 200m };
        var destination = new OrderDestination(1); // Pre-existing with OrderId = 1

        // Act
        mapper.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.OrderId); // Unchanged - constructor was not called
        Assert.Equal("New", destination.CustomerName); // Updated by mapping
        Assert.Equal(200m, destination.Total); // Updated by mapping
    }
}
