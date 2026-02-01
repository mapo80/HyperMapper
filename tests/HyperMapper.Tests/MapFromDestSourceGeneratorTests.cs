using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v10.0.0: Unit tests for MapFrom((src, dest) => ...) in Source Generator.
/// Tests MapFrom with destination parameter for computed values based on both source and destination.
/// </summary>
public class MapFromDestSourceGeneratorTests
{
    #region Test Types

    public class Order
    {
        public int Id { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class PersonDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName { get; set; } = "";
    }

    public class Product
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductDto
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class Account
    {
        public string AccountNumber { get; set; } = "";
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; }
    }

    public class AccountDto
    {
        public string AccountNumber { get; set; } = "";
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal ProjectedBalance { get; set; }
    }

    #endregion

    #region Test Profiles

    public class OrderTotalProfile : Profile
    {
        public OrderTotalProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.Tax, opt => opt.MapFrom(s => s.Subtotal * s.TaxRate))
                .ForMember(d => d.Total, opt => opt.MapFrom((src, dest) => dest.Subtotal + dest.Tax));
        }
    }

    public class PersonFullNameProfile : Profile
    {
        public PersonFullNameProfile()
        {
            CreateMap<Person, PersonDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom((src, dest) => $"{dest.FirstName} {dest.LastName}"));
        }
    }

    public class ProductTotalValueProfile : Profile
    {
        public ProductTotalValueProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.TotalValue, opt => opt.MapFrom((src, dest) => dest.Price * dest.Quantity));
        }
    }

    public class AccountProjectedBalanceProfile : Profile
    {
        public AccountProjectedBalanceProfile()
        {
            CreateMap<Account, AccountDto>()
                .ForMember(d => d.ProjectedBalance, opt => opt.MapFrom((src, dest) => dest.Balance * (1 + dest.InterestRate)));
        }
    }

    public class MixedSourceDestProfile : Profile
    {
        public MixedSourceDestProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.Tax, opt => opt.MapFrom(s => s.Subtotal * s.TaxRate))
                .ForMember(d => d.Total, opt => opt.MapFrom((src, dest) => src.Subtotal + dest.Tax - (src.Subtotal * src.DiscountPercent)));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void MapFromDest_OrderTotal_CalculatesFromDestinationProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderTotalProfile>());
        var mapper = config.CreateMapper();
        var source = new Order
        {
            Id = 1,
            Subtotal = 100m,
            TaxRate = 0.1m,  // 10%
            DiscountPercent = 0m
        };

        // Act
        var dest = mapper.Map<OrderDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(1, dest.Id);
        Assert.Equal(100m, dest.Subtotal);
        Assert.Equal(10m, dest.Tax);  // 100 * 0.1
        Assert.Equal(110m, dest.Total);  // 100 + 10
    }

    [Fact]
    public void MapFromDest_PersonFullName_CombinesDestinationProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PersonFullNameProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var dest = mapper.Map<PersonDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.FirstName);
        Assert.Equal("Doe", dest.LastName);
        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void MapFromDest_ProductTotalValue_MultipliesDestinationProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductTotalValueProfile>());
        var mapper = config.CreateMapper();
        var source = new Product
        {
            Name = "Widget",
            Price = 25.50m,
            Quantity = 4
        };

        // Act
        var dest = mapper.Map<ProductDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Widget", dest.Name);
        Assert.Equal(25.50m, dest.Price);
        Assert.Equal(4, dest.Quantity);
        Assert.Equal(102m, dest.TotalValue);  // 25.50 * 4
    }

    [Fact]
    public void MapFromDest_AccountProjectedBalance_CalculatesWithInterestRate()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AccountProjectedBalanceProfile>());
        var mapper = config.CreateMapper();
        var source = new Account
        {
            AccountNumber = "ACC-123",
            Balance = 1000m,
            InterestRate = 0.05m  // 5%
        };

        // Act
        var dest = mapper.Map<AccountDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("ACC-123", dest.AccountNumber);
        Assert.Equal(1000m, dest.Balance);
        Assert.Equal(0.05m, dest.InterestRate);
        Assert.Equal(1050m, dest.ProjectedBalance);  // 1000 * 1.05
    }

    [Fact]
    public void MapFromDest_MixedSourceDest_UsesBothSourceAndDestination()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedSourceDestProfile>());
        var mapper = config.CreateMapper();
        var source = new Order
        {
            Id = 1,
            Subtotal = 200m,
            TaxRate = 0.08m,   // 8%
            DiscountPercent = 0.1m  // 10%
        };

        // Act
        var dest = mapper.Map<OrderDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(200m, dest.Subtotal);
        Assert.Equal(16m, dest.Tax);  // 200 * 0.08
        // Total = 200 + 16 - (200 * 0.1) = 200 + 16 - 20 = 196
        Assert.Equal(196m, dest.Total);
    }

    [Fact]
    public void MapFromDest_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderTotalProfile>());
        var mapper = config.CreateMapper();
        Order? nullOrder = null;

        // Act
        var dest = mapper.Map<OrderDto>(nullOrder!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void MapFromDest_ZeroValues_HandledCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductTotalValueProfile>());
        var mapper = config.CreateMapper();
        var source = new Product
        {
            Name = "Free Item",
            Price = 0m,
            Quantity = 5
        };

        // Act
        var dest = mapper.Map<ProductDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Free Item", dest.Name);
        Assert.Equal(0m, dest.Price);
        Assert.Equal(5, dest.Quantity);
        Assert.Equal(0m, dest.TotalValue);  // 0 * 5 = 0
    }

    #endregion
}
