using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

/// <summary>
/// v12.0.0: Integration tests for IValueResolver with CodeGen (Source Generator).
/// </summary>
public class ValueResolverIntegrationTests
{
    #region Test Models

    public class VRSourceEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class VRDestinationDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string FormattedAmount { get; set; } = string.Empty;
        public VRStatusEnum StatusEnum { get; set; }
    }

    public enum VRStatusEnum
    {
        Unknown = 0,
        Active = 1,
        Inactive = 2,
        Pending = 3
    }

    #endregion

    #region Value Resolvers

    /// <summary>
    /// Resolver that combines FirstName and LastName into FullName.
    /// </summary>
    public class FullNameResolver : IValueResolver<VRSourceEntity, VRDestinationDto, string>
    {
        public string Resolve(VRSourceEntity source, VRDestinationDto destination,
            string destMember, ResolutionContext context)
        {
            return $"{source.FirstName} {source.LastName}".Trim();
        }
    }

    /// <summary>
    /// Resolver that formats amount as currency.
    /// </summary>
    public class CurrencyAmountResolver : IValueResolver<VRSourceEntity, VRDestinationDto, string>
    {
        public string Resolve(VRSourceEntity source, VRDestinationDto destination,
            string destMember, ResolutionContext context)
        {
            return source.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        }
    }

    /// <summary>
    /// Resolver that converts string Status to StatusEnum.
    /// </summary>
    public class StatusEnumResolver : IValueResolver<VRSourceEntity, VRDestinationDto, VRStatusEnum>
    {
        public VRStatusEnum Resolve(VRSourceEntity source, VRDestinationDto destination,
            VRStatusEnum destMember, ResolutionContext context)
        {
            return source.Status?.ToLowerInvariant() switch
            {
                "active" => VRStatusEnum.Active,
                "inactive" => VRStatusEnum.Inactive,
                "pending" => VRStatusEnum.Pending,
                _ => VRStatusEnum.Unknown
            };
        }
    }

    #endregion

    #region Profile

    /// <summary>
    /// Profile that uses IValueResolver for CodeGen testing.
    /// </summary>
    public class VRCodeGenProfile : Profile
    {
        public VRCodeGenProfile()
        {
            CreateMap<VRSourceEntity, VRDestinationDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>())
                .ForMember(d => d.FormattedAmount, opt => opt.MapFrom<CurrencyAmountResolver>())
                .ForMember(d => d.StatusEnum, opt => opt.MapFrom<StatusEnumResolver>());
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void CodeGen_ValueResolver_Should_Resolve_FullName()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var source = new VRSourceEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Amount = 1234.56m,
            Status = "Active"
        };

        // Act
        var result = mapper.Map<VRDestinationDto>(source);

        // Assert
        Assert.Equal("John Doe", result.FullName);
    }

    [Fact]
    public void CodeGen_ValueResolver_Should_Resolve_FormattedAmount()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var source = new VRSourceEntity
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Smith",
            Amount = 9999.99m,
            Status = "Inactive"
        };

        // Act
        var result = mapper.Map<VRDestinationDto>(source);

        // Assert
        Assert.Equal("$9,999.99", result.FormattedAmount);
    }

    [Fact]
    public void CodeGen_ValueResolver_Should_Resolve_StatusEnum()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var source = new VRSourceEntity
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Amount = 100m,
            Status = "Pending"
        };

        // Act
        var result = mapper.Map<VRDestinationDto>(source);

        // Assert
        Assert.Equal(VRStatusEnum.Pending, result.StatusEnum);
    }

    [Fact]
    public void CodeGen_ValueResolver_Should_Handle_Multiple_Resolvers()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var source = new VRSourceEntity
        {
            Id = 42,
            FirstName = "Alice",
            LastName = "Wonder",
            Amount = 500.00m,
            Status = "Active"
        };

        // Act
        var result = mapper.Map<VRDestinationDto>(source);

        // Assert
        Assert.Equal(42, result.Id);
        Assert.Equal("Alice Wonder", result.FullName);
        Assert.Equal("$500.00", result.FormattedAmount);
        Assert.Equal(VRStatusEnum.Active, result.StatusEnum);
    }

    [Fact]
    public void CodeGen_ValueResolver_Should_Handle_Empty_Names()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var source = new VRSourceEntity
        {
            Id = 1,
            FirstName = "",
            LastName = "",
            Amount = 0m,
            Status = "unknown"
        };

        // Act
        var result = mapper.Map<VRDestinationDto>(source);

        // Assert
        Assert.Equal("", result.FullName);
        Assert.Equal("$0.00", result.FormattedAmount);
        Assert.Equal(VRStatusEnum.Unknown, result.StatusEnum);
    }

    [Fact]
    public void CodeGen_ValueResolver_Should_Handle_Collection_Mapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<VRSourceEntity>
        {
            new() { Id = 1, FirstName = "One", LastName = "User", Amount = 100m, Status = "Active" },
            new() { Id = 2, FirstName = "Two", LastName = "User", Amount = 200m, Status = "Inactive" },
            new() { Id = 3, FirstName = "Three", LastName = "User", Amount = 300m, Status = "Pending" },
        };

        // Act
        var results = mapper.Map<List<VRDestinationDto>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("One User", results[0].FullName);
        Assert.Equal("Two User", results[1].FullName);
        Assert.Equal("Three User", results[2].FullName);
        Assert.Equal(VRStatusEnum.Active, results[0].StatusEnum);
        Assert.Equal(VRStatusEnum.Inactive, results[1].StatusEnum);
        Assert.Equal(VRStatusEnum.Pending, results[2].StatusEnum);
    }

    [Fact]
    public void CodeGen_ValueResolver_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        VRSourceEntity? source = null;

        // Act
        var result = mapper.Map<VRDestinationDto?>(source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CodeGen_ValueResolver_CollectionWithNulls_HandlesCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRCodeGenProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<VRSourceEntity?>
        {
            new() { Id = 1, FirstName = "First", LastName = "User", Amount = 100m, Status = "Active" },
            null,
            new() { Id = 3, FirstName = "Third", LastName = "User", Amount = 300m, Status = "Pending" }
        };

        // Act
        var results = mapper.Map<List<VRDestinationDto?>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("First User", results[0]?.FullName);
        Assert.Null(results[1]);
        Assert.Equal("Third User", results[2]?.FullName);
    }

    [Fact]
    public void CodeGen_ValueResolver_With_PreCondition_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<VRPreConditionProfile>());
        var mapper = config.CreateMapper();

        var sourceWithAmount = new VRSourceEntity
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Amount = 500m,
            Status = "Active"
        };

        var sourceWithoutAmount = new VRSourceEntity
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            Amount = 0m,
            Status = "Inactive"
        };

        // Act
        var resultWithAmount = mapper.Map<VRDestinationDto>(sourceWithAmount);
        var resultWithoutAmount = mapper.Map<VRDestinationDto>(sourceWithoutAmount);

        // Assert - PreCondition only formats amount when > 0
        Assert.Equal("$500.00", resultWithAmount.FormattedAmount);
        Assert.Equal("", resultWithoutAmount.FormattedAmount); // Default, PreCondition not met
    }

    #endregion

    #region Additional Profiles

    /// <summary>
    /// Profile with PreCondition for testing.
    /// </summary>
    public class VRPreConditionProfile : Profile
    {
        public VRPreConditionProfile()
        {
            CreateMap<VRSourceEntity, VRDestinationDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>())
                .ForMember(d => d.FormattedAmount, opt =>
                {
                    opt.PreCondition(src => src.Amount > 0);
                    opt.MapFrom<CurrencyAmountResolver>();
                })
                .ForMember(d => d.StatusEnum, opt => opt.MapFrom<StatusEnumResolver>());
        }
    }

    #endregion
}
