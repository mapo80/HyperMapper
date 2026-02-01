using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v9.0.0: Unit tests for ForPath() in Source Generator.
/// Tests nested property mapping at compile-time code generation.
/// </summary>
public class ForPathSourceGeneratorTests
{
    #region Test Types

    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string ZipCode { get; set; } = "";
    }

    public class SourceFlat
    {
        public string StreetName { get; set; } = "";
        public string CityName { get; set; } = "";
        public string PostalCode { get; set; } = "";
    }

    public class DestWithAddress
    {
        public Address Address { get; set; } = new();
    }

    public class SourceWithMultiplePaths
    {
        public string Street1 { get; set; } = "";
        public string Street2 { get; set; } = "";
    }

    public class DestWithTwoAddresses
    {
        public Address Home { get; set; } = new();
        public Address Work { get; set; } = new();
    }

    public class InnerDetails
    {
        public string Value { get; set; } = "";
    }

    public class MiddleLevel
    {
        public InnerDetails Details { get; set; } = new();
    }

    public class SourceDeep
    {
        public string DeepValue { get; set; } = "";
    }

    public class DestDeep
    {
        public MiddleLevel Middle { get; set; } = new();
    }

    public class SourceWithId
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Street { get; set; } = "";
    }

    public class DestMixedWithPath
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Address Address { get; set; } = new();
    }

    #endregion

    #region Test Profiles

    public class SinglePathProfile : Profile
    {
        public SinglePathProfile()
        {
            CreateMap<SourceFlat, DestWithAddress>()
                .ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.StreetName));
        }
    }

    public class MultiplePathsProfile : Profile
    {
        public MultiplePathsProfile()
        {
            CreateMap<SourceFlat, DestWithAddress>()
                .ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.StreetName))
                .ForPath(d => d.Address.City, opt => opt.MapFrom(s => s.CityName))
                .ForPath(d => d.Address.ZipCode, opt => opt.MapFrom(s => s.PostalCode));
        }
    }

    public class TwoObjectPathsProfile : Profile
    {
        public TwoObjectPathsProfile()
        {
            CreateMap<SourceWithMultiplePaths, DestWithTwoAddresses>()
                .ForPath(d => d.Home.Street, opt => opt.MapFrom(s => s.Street1))
                .ForPath(d => d.Work.Street, opt => opt.MapFrom(s => s.Street2));
        }
    }

    public class DeepPathProfile : Profile
    {
        public DeepPathProfile()
        {
            CreateMap<SourceDeep, DestDeep>()
                .ForPath(d => d.Middle.Details.Value, opt => opt.MapFrom(s => s.DeepValue));
        }
    }

    public class MixedPathAndMemberProfile : Profile
    {
        public MixedPathAndMemberProfile()
        {
            CreateMap<SourceWithId, DestMixedWithPath>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
                .ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.Street));
        }
    }

    public class PathWithTransformationProfile : Profile
    {
        public PathWithTransformationProfile()
        {
            CreateMap<SourceFlat, DestWithAddress>()
                .ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.StreetName.ToUpper()));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void ForPath_SingleLevel_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SinglePathProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceFlat { StreetName = "123 Main St" };

        // Act
        var dest = mapper.Map<DestWithAddress>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Address);
        Assert.Equal("123 Main St", dest.Address.Street);
    }

    [Fact]
    public void ForPath_MultiplePaths_SameObject_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultiplePathsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceFlat
        {
            StreetName = "456 Oak Ave",
            CityName = "Springfield",
            PostalCode = "12345"
        };

        // Act
        var dest = mapper.Map<DestWithAddress>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Address);
        Assert.Equal("456 Oak Ave", dest.Address.Street);
        Assert.Equal("Springfield", dest.Address.City);
        Assert.Equal("12345", dest.Address.ZipCode);
    }

    [Fact]
    public void ForPath_TwoSeparateObjects_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TwoObjectPathsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMultiplePaths
        {
            Street1 = "Home Street",
            Street2 = "Work Street"
        };

        // Act
        var dest = mapper.Map<DestWithTwoAddresses>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Home);
        Assert.NotNull(dest.Work);
        Assert.Equal("Home Street", dest.Home.Street);
        Assert.Equal("Work Street", dest.Work.Street);
    }

    [Fact]
    public void ForPath_DeepNesting_CreatesIntermediates()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepPathProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceDeep { DeepValue = "Deep nested value" };

        // Act
        var dest = mapper.Map<DestDeep>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Middle);
        Assert.NotNull(dest.Middle.Details);
        Assert.Equal("Deep nested value", dest.Middle.Details.Value);
    }

    [Fact]
    public void ForPath_MixedWithForMember_BothWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedPathAndMemberProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithId
        {
            Id = 42,
            Name = "Test",
            Street = "Mixed Street"
        };

        // Act
        var dest = mapper.Map<DestMixedWithPath>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Id);
        Assert.Equal("Test", dest.Name);
        Assert.NotNull(dest.Address);
        Assert.Equal("Mixed Street", dest.Address.Street);
    }

    [Fact]
    public void ForPath_WithTransformation_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PathWithTransformationProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceFlat { StreetName = "lowercase street" };

        // Act
        var dest = mapper.Map<DestWithAddress>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Address);
        Assert.Equal("LOWERCASE STREET", dest.Address.Street);
    }

    [Fact]
    public void ForPath_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SinglePathProfile>());
        var mapper = config.CreateMapper();
        SourceFlat? nullSource = null;

        // Act
        var dest = mapper.Map<DestWithAddress>(nullSource!);

        // Assert
        Assert.Null(dest);
    }

    #endregion
}
