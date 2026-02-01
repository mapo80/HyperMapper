using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 ValidateMemberList() feature - Control unmapped member validation.
/// AutoMapper API compatibility: CreateMap<S, D>().ValidateMemberList(MemberList.None)
/// </summary>
public class ValidateMemberListTests
{
    #region Test Models

    public class SourceWithExtra
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExtraSourceProperty { get; set; }
    }

    public class DestWithExtra
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExtraDestProperty { get; set; }
    }

    public class MatchingSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class MatchingDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class SourceForIgnore
    {
        public int Id { get; set; }
    }

    public class DestForIgnore
    {
        public int Id { get; set; }
        public string? UnmappedProperty { get; set; }
    }

    public class SourceWithNested
    {
        public int Id { get; set; }
        public AddressSource? Address { get; set; }
    }

    public class AddressSource
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    public class DestWithFlattened
    {
        public int Id { get; set; }
        public string? AddressStreet { get; set; }
        public string? AddressCity { get; set; }
    }

    #endregion

    #region Profiles

    public class DefaultValidationProfile : Profile
    {
        public DefaultValidationProfile()
        {
            // Default is MemberList.Destination
            CreateMap<SourceForIgnore, DestForIgnore>();
        }
    }

    public class NoneValidationProfile : Profile
    {
        public NoneValidationProfile()
        {
            CreateMap<SourceForIgnore, DestForIgnore>()
                .ValidateMemberList(MemberList.None);
        }
    }

    public class DestinationValidationProfile : Profile
    {
        public DestinationValidationProfile()
        {
            CreateMap<SourceForIgnore, DestForIgnore>()
                .ValidateMemberList(MemberList.Destination);
        }
    }

    public class SourceValidationProfile : Profile
    {
        public SourceValidationProfile()
        {
            CreateMap<SourceWithExtra, MatchingDest>()
                .ValidateMemberList(MemberList.Source);
        }
    }

    public class DestinationWithIgnoreProfile : Profile
    {
        public DestinationWithIgnoreProfile()
        {
            CreateMap<SourceForIgnore, DestForIgnore>()
                .ForMember(d => d.UnmappedProperty, opt => opt.Ignore());
        }
    }

    public class MatchingProfile : Profile
    {
        public MatchingProfile()
        {
            CreateMap<MatchingSource, MatchingDest>();
        }
    }

    public class FlatteningValidationProfile : Profile
    {
        public FlatteningValidationProfile()
        {
            CreateMap<SourceWithNested, DestWithFlattened>();
        }
    }

    public class SourceMappedExplicitlyProfile : Profile
    {
        public SourceMappedExplicitlyProfile()
        {
            CreateMap<SourceWithExtra, MatchingDest>()
                .ValidateMemberList(MemberList.Source)
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.ExtraSourceProperty));
        }
    }

    #endregion

    [Fact]
    public void ValidateMemberList_Default_IsDestination()
    {
        // Arrange - Default is MemberList.Destination, so unmapped dest member should fail
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DefaultValidationProfile>());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => config.AssertConfigurationIsValid());
        Assert.Contains("UnmappedProperty", ex.Message);
        Assert.Contains("destination type", ex.Message);
    }

    [Fact]
    public void ValidateMemberList_None_SkipsValidation()
    {
        // Arrange - MemberList.None skips all validation
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NoneValidationProfile>());

        // Act & Assert - Should NOT throw even with unmapped destination property
        config.AssertConfigurationIsValid(); // Does not throw
    }

    [Fact]
    public void ValidateMemberList_Destination_ValidatesDestMembers()
    {
        // Arrange - Explicit MemberList.Destination
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DestinationValidationProfile>());

        // Act & Assert - Should throw for unmapped destination property
        var ex = Assert.Throws<InvalidOperationException>(() => config.AssertConfigurationIsValid());
        Assert.Contains("UnmappedProperty", ex.Message);
    }

    [Fact]
    public void ValidateMemberList_Source_ValidatesSourceMembers()
    {
        // Arrange - MemberList.Source validates source members
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SourceValidationProfile>());

        // Act & Assert - Should throw for unmapped source property
        var ex = Assert.Throws<InvalidOperationException>(() => config.AssertConfigurationIsValid());
        Assert.Contains("ExtraSourceProperty", ex.Message);
        Assert.Contains("source member", ex.Message);
    }

    [Fact]
    public void ValidateMemberList_WithIgnore_PassesValidation()
    {
        // Arrange - Using Ignore() should satisfy validation
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DestinationWithIgnoreProfile>());

        // Act & Assert - Should NOT throw because unmapped property is explicitly ignored
        config.AssertConfigurationIsValid(); // Does not throw
    }

    [Fact]
    public void ValidateMemberList_AllMembersMatch_PassesValidation()
    {
        // Arrange - When all members have matches, validation passes
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MatchingProfile>());

        // Act & Assert
        config.AssertConfigurationIsValid(); // Does not throw
    }

    [Fact]
    public void ValidateMemberList_WithFlattening_PassesValidation()
    {
        // Arrange - Flattening convention should satisfy validation
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FlatteningValidationProfile>());

        // Act & Assert - Should NOT throw because AddressStreet/AddressCity match via flattening
        config.AssertConfigurationIsValid(); // Does not throw
    }

    [Fact]
    public void ValidateMemberList_Source_WithExplicitMapping_Passes()
    {
        // Arrange - All source members are used in explicit mappings
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SourceMappedExplicitlyProfile>());

        // Act & Assert - Should NOT throw because all source members are mapped
        config.AssertConfigurationIsValid(); // Does not throw
    }

    [Fact]
    public void ValidateMemberList_MappingStillWorks_RegardlessOfValidation()
    {
        // Arrange - Validation setting doesn't affect actual mapping
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NoneValidationProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForIgnore { Id = 42 };

        // Act
        var result = mapper.Map<DestForIgnore>(source);

        // Assert - Mapping should work even with MemberList.None
        Assert.Equal(42, result.Id);
        Assert.Null(result.UnmappedProperty); // Not mapped, remains null
    }

    [Fact]
    public void ValidateMemberList_ErrorMessage_IncludesHelpfulInfo()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DefaultValidationProfile>());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => config.AssertConfigurationIsValid());

        // Assert - Error message should be helpful
        Assert.Contains("ForMember", ex.Message);
        Assert.Contains("Ignore", ex.Message);
    }
}
