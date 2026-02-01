using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 ForCtorParam() feature - Constructor parameter mapping.
/// AutoMapper API compatibility: CreateMap<S, D>().ForCtorParam("name", opt => opt.MapFrom(s => s.FullName))
/// </summary>
public class ForCtorParamTests
{
    #region Test Models

    public class PersonSource
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Age { get; set; }
    }

    public class PersonWithCtor
    {
        public string Name { get; }
        public int Age { get; set; }

        public PersonWithCtor(string name)
        {
            Name = name;
        }
    }

    public class PersonWithMultipleCtorParams
    {
        public string Name { get; }
        public int Age { get; }

        public PersonWithMultipleCtorParams(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }

    public class SourceWithId
    {
        public int Identifier { get; set; }
        public string? Description { get; set; }
    }

    public class DestWithId
    {
        public int Id { get; }
        public string? Description { get; set; }

        public DestWithId(int id)
        {
            Id = id;
        }
    }

    public class OptionalParamSource
    {
        public string? Name { get; set; }
    }

    public class OptionalParamDest
    {
        public string Name { get; }
        public string Tag { get; }

        public OptionalParamDest(string name, string tag = "default")
        {
            Name = name;
            Tag = tag;
        }
    }

    public class ConventionSource
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class ConventionDest
    {
        public string Name { get; }
        public int Value { get; }
        public string? Extra { get; set; }

        public ConventionDest(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public class MultiConstructorDest
    {
        public int Id { get; }
        public string? Name { get; set; }

        public MultiConstructorDest()
        {
            Id = 0;
        }

        public MultiConstructorDest(int id)
        {
            Id = id;
        }

        public MultiConstructorDest(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    #endregion

    #region Profiles

    public class SingleCtorParamProfile : Profile
    {
        public SingleCtorParamProfile()
        {
            CreateMap<PersonSource, PersonWithCtor>()
                .ForCtorParam("name", opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
        }
    }

    public class MultipleCtorParamsProfile : Profile
    {
        public MultipleCtorParamsProfile()
        {
            CreateMap<PersonSource, PersonWithMultipleCtorParams>()
                .ForCtorParam("name", opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForCtorParam("age", opt => opt.MapFrom(s => s.Age));
        }
    }

    public class RenamedCtorParamProfile : Profile
    {
        public RenamedCtorParamProfile()
        {
            CreateMap<SourceWithId, DestWithId>()
                .ForCtorParam("id", opt => opt.MapFrom(s => s.Identifier));
        }
    }

    public class OptionalCtorParamProfile : Profile
    {
        public OptionalCtorParamProfile()
        {
            CreateMap<OptionalParamSource, OptionalParamDest>()
                .ForCtorParam("name", opt => opt.MapFrom(s => s.Name!));
            // 'tag' parameter uses default value
        }
    }

    public class ConventionCtorProfile : Profile
    {
        public ConventionCtorProfile()
        {
            // With ForCtorParam for constructor parameters
            CreateMap<ConventionSource, ConventionDest>()
                .ForCtorParam("name", opt => opt.MapFrom(s => s.Name))
                .ForCtorParam("value", opt => opt.MapFrom(s => s.Value));
        }
    }

    public class MixedCtorProfile : Profile
    {
        public MixedCtorProfile()
        {
            // One ForCtorParam, one convention
            CreateMap<ConventionSource, ConventionDest>()
                .ForCtorParam("name", opt => opt.MapFrom(s => s.Name.ToUpper()));
        }
    }

    public class ForCtorParamWithForMemberProfile : Profile
    {
        public ForCtorParamWithForMemberProfile()
        {
            CreateMap<PersonSource, PersonWithCtor>()
                .ForCtorParam("name", opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
                .ForMember(d => d.Age, opt => opt.MapFrom(s => s.Age * 2));
        }
    }

    public class BestConstructorProfile : Profile
    {
        public BestConstructorProfile()
        {
            CreateMap<SourceWithId, MultiConstructorDest>()
                .ForCtorParam("id", opt => opt.MapFrom(s => s.Identifier));
        }
    }

    #endregion

    [Fact]
    public void ForCtorParam_SingleParameter_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new PersonSource
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30
        };

        // Act
        var result = mapper.Map<PersonWithCtor>(source);

        // Assert
        Assert.Equal("John Doe", result.Name);
        Assert.Equal(30, result.Age); // Convention mapping for settable property
    }

    [Fact]
    public void ForCtorParam_MultipleParameters_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleCtorParamsProfile>());
        var mapper = config.CreateMapper();
        var source = new PersonSource
        {
            FirstName = "Jane",
            LastName = "Smith",
            Age = 25
        };

        // Act
        var result = mapper.Map<PersonWithMultipleCtorParams>(source);

        // Assert
        Assert.Equal("Jane Smith", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void ForCtorParam_RenamedParameter_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RenamedCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithId
        {
            Identifier = 42,
            Description = "Test"
        };

        // Act
        var result = mapper.Map<DestWithId>(source);

        // Assert
        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Description);
    }

    [Fact]
    public void ForCtorParam_OptionalParameter_UsesDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OptionalCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new OptionalParamSource { Name = "Test" };

        // Act
        var result = mapper.Map<OptionalParamDest>(source);

        // Assert
        Assert.Equal("Test", result.Name);
        Assert.Equal("default", result.Tag); // Uses default value
    }

    [Fact]
    public void ForCtorParam_Convention_MapsMatchingNames()
    {
        // Arrange - No ForCtorParam, relies on convention
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConventionCtorProfile>());
        var mapper = config.CreateMapper();
        var source = new ConventionSource { Name = "ConventionName", Value = 99 };

        // Act
        var result = mapper.Map<ConventionDest>(source);

        // Assert
        Assert.Equal("ConventionName", result.Name);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public void ForCtorParam_Mixed_ExplicitOverridesConvention()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedCtorProfile>());
        var mapper = config.CreateMapper();
        var source = new ConventionSource { Name = "mixedtest", Value = 50 };

        // Act
        var result = mapper.Map<ConventionDest>(source);

        // Assert
        Assert.Equal("MIXEDTEST", result.Name); // ForCtorParam with ToUpper()
        Assert.Equal(50, result.Value); // Convention
    }

    [Fact]
    public void ForCtorParam_WithForMember_BothWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForCtorParamWithForMemberProfile>());
        var mapper = config.CreateMapper();
        var source = new PersonSource
        {
            FirstName = "Test",
            LastName = "User",
            Age = 20
        };

        // Act
        var result = mapper.Map<PersonWithCtor>(source);

        // Assert
        Assert.Equal("Test User", result.Name); // ForCtorParam
        Assert.Equal(40, result.Age); // ForMember: Age * 2
    }

    [Fact]
    public void ForCtorParam_BestConstructorSelection_UsesConfigured()
    {
        // Arrange - Should use constructor(int id) not default constructor
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BestConstructorProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithId { Identifier = 123, Description = "Test" };

        // Act
        var result = mapper.Map<MultiConstructorDest>(source);

        // Assert
        Assert.Equal(123, result.Id);
    }

    [Fact]
    public void ForCtorParam_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SingleCtorParamProfile>());
        var mapper = config.CreateMapper();
        PersonSource? nullSource = null;

        // Act
        var result = mapper.Map<PersonWithCtor>(nullSource!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ForCtorParam_Collection_EachElementUsesConstructor()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleCtorParamsProfile>());
        var mapper = config.CreateMapper();
        var sources = new List<PersonSource>
        {
            new() { FirstName = "A", LastName = "1", Age = 10 },
            new() { FirstName = "B", LastName = "2", Age = 20 }
        };

        // Act
        var results = mapper.Map<List<PersonWithMultipleCtorParams>>(sources);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("A 1", results[0].Name);
        Assert.Equal(10, results[0].Age);
        Assert.Equal("B 2", results[1].Name);
        Assert.Equal(20, results[1].Age);
    }
}
