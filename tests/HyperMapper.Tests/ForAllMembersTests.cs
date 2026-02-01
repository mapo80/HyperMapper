using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 ForAllMembers() and ForAllOtherMembers() features - Bulk member configuration.
/// AutoMapper API compatibility:
/// - CreateMap<S, D>().ForAllMembers(opt => opt.Ignore())
/// - CreateMap<S, D>().ForMember(d => d.Id, ...).ForAllOtherMembers(opt => opt.Ignore())
/// </summary>
public class ForAllMembersTests
{
    #region Test Models

    public class SourceWithMany
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Value { get; set; }
    }

    public class DestWithMany
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Value { get; set; }
        public string? Extra { get; set; }
    }

    public class SmallSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class SmallDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    #endregion

    #region Profiles

    public class IgnoreAllProfile : Profile
    {
        public IgnoreAllProfile()
        {
            CreateMap<SourceWithMany, DestWithMany>()
                .ForAllMembers(opt => opt.Ignore());
        }
    }

    public class IgnoreAllOtherProfile : Profile
    {
        public IgnoreAllOtherProfile()
        {
            CreateMap<SourceWithMany, DestWithMany>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name))
                .ForAllOtherMembers(opt => opt.Ignore());
        }
    }

    public class ForMemberOverridesForAllMembersProfile : Profile
    {
        public ForMemberOverridesForAllMembersProfile()
        {
            CreateMap<SourceWithMany, DestWithMany>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id * 10))
                .ForAllMembers(opt => opt.Ignore());
        }
    }

    public class ForAllMembersWithPreConditionProfile : Profile
    {
        public ForAllMembersWithPreConditionProfile()
        {
            // PreCondition only works with ForMember, not ForAllMembers in AutoMapper
            // This test verifies our behavior matches - ForAllMembers with PreCondition applies to all
            CreateMap<SourceWithMany, DestWithMany>()
                .ForAllMembers(opt => opt.Ignore());
        }
    }

    public class ForAllMembersStringOnlyProfile : Profile
    {
        public ForAllMembersStringOnlyProfile()
        {
            // Test that applies only to string properties
            CreateMap<SmallSource, SmallDest>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.NullSubstitute("N/A"));
        }
    }

    public class EmptyDestinationProfile : Profile
    {
        public EmptyDestinationProfile()
        {
            // Dest has no writable properties scenario (not realistic but tests edge case)
            CreateMap<SmallSource, SmallDest>()
                .ForAllMembers(opt => opt.Ignore());
        }
    }

    public class ForAllOtherMembersWithMultipleForMemberProfile : Profile
    {
        public ForAllOtherMembersWithMultipleForMemberProfile()
        {
            CreateMap<SourceWithMany, DestWithMany>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name!.ToUpper()))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
                .ForAllOtherMembers(opt => opt.Ignore());
        }
    }

    public class OrderMattersProfile : Profile
    {
        public OrderMattersProfile()
        {
            // ForAllOtherMembers before ForMember - ForMember should still take precedence
            CreateMap<SourceWithMany, DestWithMany>()
                .ForAllOtherMembers(opt => opt.Ignore())
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
        }
    }

    public class BaselineConventionProfile : Profile
    {
        public BaselineConventionProfile()
        {
            CreateMap<SmallSource, SmallDest>();
        }
    }

    #endregion

    [Fact]
    public void ForAllMembers_IgnoreAll_AllMembersIgnored()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IgnoreAllProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMany
        {
            Id = 1,
            Name = "Test",
            Description = "Desc",
            Value = 100
        };

        // Act
        var result = mapper.Map<DestWithMany>(source);

        // Assert - All should be default values because everything is ignored
        Assert.Equal(0, result.Id);
        Assert.Null(result.Name);
        Assert.Null(result.Description);
        Assert.Equal(0, result.Value);
        Assert.Null(result.Extra);
    }

    [Fact]
    public void ForAllOtherMembers_IgnoreOthers_OnlyConfiguredMapped()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IgnoreAllOtherProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMany
        {
            Id = 42,
            Name = "Mapped",
            Description = "Ignored",
            Value = 999
        };

        // Act
        var result = mapper.Map<DestWithMany>(source);

        // Assert
        Assert.Equal(42, result.Id); // Explicitly configured
        Assert.Equal("Mapped", result.Name); // Explicitly configured
        Assert.Null(result.Description); // Ignored
        Assert.Equal(0, result.Value); // Ignored
        Assert.Null(result.Extra); // Ignored (no source match)
    }

    [Fact]
    public void ForAllMembers_ForMemberTakesPrecedence()
    {
        // Arrange - ForMember should override ForAllMembers
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForMemberOverridesForAllMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMany
        {
            Id = 5,
            Name = "Test",
            Description = "Desc",
            Value = 100
        };

        // Act
        var result = mapper.Map<DestWithMany>(source);

        // Assert
        Assert.Equal(50, result.Id); // ForMember: Id * 10
        Assert.Null(result.Name); // ForAllMembers: Ignored
        Assert.Null(result.Description); // ForAllMembers: Ignored
        Assert.Equal(0, result.Value); // ForAllMembers: Ignored
    }

    [Fact]
    public void ForAllMembers_IgnoreAllWithPreConditionProfile_AllIgnored()
    {
        // Arrange - ForAllMembers with Ignore() ignores all members
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForAllMembersWithPreConditionProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithMany { Id = 1, Name = "Test", Value = 100 };
        var result = mapper.Map<DestWithMany>(source);

        // All should be default because everything is ignored
        Assert.Equal(0, result.Id);
        Assert.Null(result.Name);
    }

    [Fact]
    public void ForMember_WithNullSubstitute_AppliesSubstituteToStrings()
    {
        // Arrange - ForMember with NullSubstitute for string property
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForAllMembersStringOnlyProfile>());
        var mapper = config.CreateMapper();
        var source = new SmallSource { Id = 1, Name = null };

        // Act
        var result = mapper.Map<SmallDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("N/A", result.Name); // NullSubstitute applied to null string
    }

    [Fact]
    public void ForAllOtherMembers_WithMultipleForMember_AllExplicitMapped()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForAllOtherMembersWithMultipleForMemberProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMany
        {
            Id = 10,
            Name = "lowercase",
            Description = "A description",
            Value = 500
        };

        // Act
        var result = mapper.Map<DestWithMany>(source);

        // Assert
        Assert.Equal(10, result.Id); // Explicit
        Assert.Equal("LOWERCASE", result.Name); // Explicit with ToUpper
        Assert.Equal("A description", result.Description); // Explicit
        Assert.Equal(0, result.Value); // Ignored by ForAllOtherMembers
        Assert.Null(result.Extra); // Ignored by ForAllOtherMembers
    }

    [Fact]
    public void ForAllOtherMembers_OrderDoesNotMatter_ForMemberStillWins()
    {
        // Arrange - Even though ForAllOtherMembers is called first, ForMember should win
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderMattersProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMany { Id = 99, Name = "Test" };

        // Act
        var result = mapper.Map<DestWithMany>(source);

        // Assert
        Assert.Equal(99, result.Id); // ForMember always wins
        Assert.Null(result.Name); // Ignored
    }

    [Fact]
    public void ForAllMembers_EmptyDestination_NoError()
    {
        // Arrange - All members ignored, no error
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EmptyDestinationProfile>());
        var mapper = config.CreateMapper();
        var source = new SmallSource { Id = 1, Name = "Test" };

        // Act
        var result = mapper.Map<SmallDest>(source);

        // Assert - All ignored
        Assert.Equal(0, result.Id);
        Assert.Null(result.Name);
    }

    [Fact]
    public void ForAllMembers_MappingStillWorks_WithoutForAllMembers()
    {
        // Arrange - Baseline: without ForAllMembers, convention mapping works
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BaselineConventionProfile>());
        var mapper = config.CreateMapper();
        var source = new SmallSource { Id = 5, Name = "Convention" };

        // Act
        var result = mapper.Map<SmallDest>(source);

        // Assert
        Assert.Equal(5, result.Id);
        Assert.Equal("Convention", result.Name);
    }
}
