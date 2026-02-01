using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for Source Generator support of enhanced MapFrom expressions.
/// These tests verify that complex MapFrom lambda expressions are correctly
/// processed and generate working code at compile-time.
/// </summary>
public class MapFromEnhancedSourceGeneratorTests
{
    #region Test Types

    public class SourceForArithmetic
    {
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class DestForArithmetic
    {
        public decimal Total { get; set; }
    }

    public class SourceForTernary
    {
        public bool Active { get; set; }
        public string? Name { get; set; }
    }

    public class DestForTernary
    {
        public string? DisplayName { get; set; }
    }

    public class SourceForNullCoalescing
    {
        public string? Name { get; set; }
    }

    public class DestForNullCoalescing
    {
        public string? DisplayName { get; set; }
    }

    public class SourceForInterpolation
    {
        public string? First { get; set; }
        public string? Last { get; set; }
    }

    public class DestForInterpolation
    {
        public string? FullName { get; set; }
    }

    public class OrderItem
    {
        public decimal Price { get; set; }
    }

    public class SourceForLinqCount
    {
        public List<string>? Items { get; set; }
    }

    public class DestForLinqCount
    {
        public int ItemCount { get; set; }
    }

    public class SourceForLinqSum
    {
        public List<OrderItem>? Items { get; set; }
    }

    public class DestForLinqSum
    {
        public decimal TotalPrice { get; set; }
    }

    public class SourceForLinqFirst
    {
        public List<OrderItem>? Items { get; set; }
    }

    public class DestForLinqFirst
    {
        public decimal? FirstPrice { get; set; }
    }

    public class SourceForMethodCall
    {
        public string? Name { get; set; }
    }

    public class DestForMethodCall
    {
        public string? UpperName { get; set; }
    }

    public class SourceForChainedMethod
    {
        public string? Name { get; set; }
    }

    public class DestForChainedMethod
    {
        public string? ProcessedName { get; set; }
    }

    public class Address
    {
        public string? City { get; set; }
    }

    public class SourceForNested
    {
        public Address? Address { get; set; }
    }

    public class DestForNested
    {
        public string? City { get; set; }
    }

    public class Company
    {
        public Address? Address { get; set; }
    }

    public class SourceForDeepNested
    {
        public Company? Company { get; set; }
    }

    public class DestForDeepNested
    {
        public string? City { get; set; }
    }

    #endregion

    #region Test Profiles

    public class ArithmeticProfile : Profile
    {
        public ArithmeticProfile()
        {
            CreateMap<SourceForArithmetic, DestForArithmetic>()
                .ForMember(d => d.Total, opt => opt.MapFrom(s => s.Price * s.Quantity));
        }
    }

    public class TernaryProfile : Profile
    {
        public TernaryProfile()
        {
            CreateMap<SourceForTernary, DestForTernary>()
                .ForMember(d => d.DisplayName, opt => opt.MapFrom(s => s.Active ? s.Name : "N/A"));
        }
    }

    public class NullCoalescingProfile : Profile
    {
        public NullCoalescingProfile()
        {
            CreateMap<SourceForNullCoalescing, DestForNullCoalescing>()
                .ForMember(d => d.DisplayName, opt => opt.MapFrom(s => s.Name ?? "Unknown"));
        }
    }

    public class InterpolationProfile : Profile
    {
        public InterpolationProfile()
        {
            CreateMap<SourceForInterpolation, DestForInterpolation>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.First} {s.Last}"));
        }
    }

    public class LinqCountProfile : Profile
    {
        public LinqCountProfile()
        {
            CreateMap<SourceForLinqCount, DestForLinqCount>()
                .ForMember(d => d.ItemCount, opt => opt.MapFrom(s => s.Items != null ? s.Items.Count : 0));
        }
    }

    public class LinqSumProfile : Profile
    {
        public LinqSumProfile()
        {
            CreateMap<SourceForLinqSum, DestForLinqSum>()
                .ForMember(d => d.TotalPrice, opt => opt.MapFrom(s => s.Items != null ? s.Items.Sum(x => x.Price) : 0m));
        }
    }

    public class LinqFirstProfile : Profile
    {
        public LinqFirstProfile()
        {
            CreateMap<SourceForLinqFirst, DestForLinqFirst>()
                .ForMember(d => d.FirstPrice, opt => opt.MapFrom(s => s.Items != null && s.Items.Count > 0 ? s.Items[0].Price : (decimal?)null));
        }
    }

    public class MethodCallProfile : Profile
    {
        public MethodCallProfile()
        {
            CreateMap<SourceForMethodCall, DestForMethodCall>()
                .ForMember(d => d.UpperName, opt => opt.MapFrom(s => s.Name != null ? s.Name.ToUpper() : null));
        }
    }

    public class ChainedMethodProfile : Profile
    {
        public ChainedMethodProfile()
        {
            CreateMap<SourceForChainedMethod, DestForChainedMethod>()
                .ForMember(d => d.ProcessedName, opt => opt.MapFrom(s => s.Name != null ? s.Name.Trim().ToLower() : null));
        }
    }

    public class NestedPropertyProfile : Profile
    {
        public NestedPropertyProfile()
        {
            CreateMap<SourceForNested, DestForNested>()
                .ForMember(d => d.City, opt => opt.MapFrom(s => s.Address != null ? s.Address.City : null));
        }
    }

    public class DeepNestedPropertyProfile : Profile
    {
        public DeepNestedPropertyProfile()
        {
            CreateMap<SourceForDeepNested, DestForDeepNested>()
                .ForMember(d => d.City, opt => opt.MapFrom(s => s.Company != null && s.Company.Address != null ? s.Company.Address.City : null));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void MapFrom_Arithmetic_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ArithmeticProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForArithmetic { Price = 10.5m, Quantity = 3 };

        // Act
        var dest = mapper.Map<DestForArithmetic>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(31.5m, dest.Total);
    }

    [Fact]
    public void MapFrom_Ternary_WhenTrue_UsesFirstValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TernaryProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForTernary { Active = true, Name = "John" };

        // Act
        var dest = mapper.Map<DestForTernary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.DisplayName);
    }

    [Fact]
    public void MapFrom_Ternary_WhenFalse_UsesSecondValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TernaryProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForTernary { Active = false, Name = "John" };

        // Act
        var dest = mapper.Map<DestForTernary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("N/A", dest.DisplayName);
    }

    [Fact]
    public void MapFrom_NullCoalescing_WhenNotNull_UsesValue()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForNullCoalescing { Name = "John" };

        // Act
        var dest = mapper.Map<DestForNullCoalescing>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.DisplayName);
    }

    [Fact]
    public void MapFrom_NullCoalescing_WhenNull_UsesDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullCoalescingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForNullCoalescing { Name = null };

        // Act
        var dest = mapper.Map<DestForNullCoalescing>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Unknown", dest.DisplayName);
    }

    [Fact]
    public void MapFrom_StringInterpolation_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<InterpolationProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForInterpolation { First = "John", Last = "Doe" };

        // Act
        var dest = mapper.Map<DestForInterpolation>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void MapFrom_LinqCount_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinqCountProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForLinqCount { Items = new List<string> { "A", "B", "C" } };

        // Act
        var dest = mapper.Map<DestForLinqCount>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(3, dest.ItemCount);
    }

    [Fact]
    public void MapFrom_LinqSum_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinqSumProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForLinqSum
        {
            Items = new List<OrderItem>
            {
                new OrderItem { Price = 10m },
                new OrderItem { Price = 20m },
                new OrderItem { Price = 15m }
            }
        };

        // Act
        var dest = mapper.Map<DestForLinqSum>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(45m, dest.TotalPrice);
    }

    [Fact]
    public void MapFrom_LinqFirst_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinqFirstProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForLinqFirst
        {
            Items = new List<OrderItem>
            {
                new OrderItem { Price = 99.99m },
                new OrderItem { Price = 50m }
            }
        };

        // Act
        var dest = mapper.Map<DestForLinqFirst>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(99.99m, dest.FirstPrice);
    }

    [Fact]
    public void MapFrom_MethodCall_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MethodCallProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForMethodCall { Name = "hello" };

        // Act
        var dest = mapper.Map<DestForMethodCall>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("HELLO", dest.UpperName);
    }

    [Fact]
    public void MapFrom_ChainedMethodCalls_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ChainedMethodProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForChainedMethod { Name = "  HELLO WORLD  " };

        // Act
        var dest = mapper.Map<DestForChainedMethod>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("hello world", dest.ProcessedName);
    }

    [Fact]
    public void MapFrom_NestedProperty_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedPropertyProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForNested { Address = new Address { City = "New York" } };

        // Act
        var dest = mapper.Map<DestForNested>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("New York", dest.City);
    }

    [Fact]
    public void MapFrom_DeepNestedProperty_GeneratesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepNestedPropertyProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForDeepNested
        {
            Company = new Company { Address = new Address { City = "San Francisco" } }
        };

        // Act
        var dest = mapper.Map<DestForDeepNested>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("San Francisco", dest.City);
    }

    #endregion
}
