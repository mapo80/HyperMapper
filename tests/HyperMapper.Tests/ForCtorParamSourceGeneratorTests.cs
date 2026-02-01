using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v9.0.0: Unit tests for ForCtorParam() in Source Generator.
/// Tests constructor parameter mapping at compile-time code generation.
/// </summary>
public class ForCtorParamSourceGeneratorTests
{
    #region Test Types

    public class PersonSource
    {
        public int PersonId { get; set; }
        public string FullName { get; set; } = "";
        public int Age { get; set; }
    }

    public class PersonDest
    {
        public int Id { get; }
        public string Name { get; }
        public int Age { get; set; }

        public PersonDest(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class SourceWithMultipleProps
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public string Text { get; set; } = "";
    }

    public class DestWithThreeCtorParams
    {
        public int A { get; }
        public int B { get; }
        public string C { get; }

        public DestWithThreeCtorParams(int a, int b, string c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    public class SourceSameNames
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DestSameNames
    {
        public int Id { get; }
        public string Name { get; }

        public DestSameNames(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class SourceWithOptional
    {
        public string Title { get; set; } = "";
    }

    public class DestWithOptionalParam
    {
        public string Title { get; }
        public int Count { get; }

        public DestWithOptionalParam(string title, int count = 10)
        {
            Title = title;
            Count = count;
        }
    }

    public class SourceMixed
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class DestMixed
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; set; } = "";

        public DestMixed(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    #endregion

    #region Test Profiles

    public class RenamedCtorParamProfile : Profile
    {
        public RenamedCtorParamProfile()
        {
            CreateMap<PersonSource, PersonDest>()
                .ForCtorParam("id", opt => opt.MapFrom(s => s.PersonId))
                .ForCtorParam("name", opt => opt.MapFrom(s => s.FullName))
                .ForMember(d => d.Age, opt => opt.MapFrom(s => s.Age));
        }
    }

    public class MultipleCtorParamsProfile : Profile
    {
        public MultipleCtorParamsProfile()
        {
            CreateMap<SourceWithMultipleProps, DestWithThreeCtorParams>()
                .ForCtorParam("a", opt => opt.MapFrom(s => s.Value1))
                .ForCtorParam("b", opt => opt.MapFrom(s => s.Value2))
                .ForCtorParam("c", opt => opt.MapFrom(s => s.Text));
        }
    }

    public class SameNameCtorParamProfile : Profile
    {
        public SameNameCtorParamProfile()
        {
            CreateMap<SourceSameNames, DestSameNames>()
                .ForCtorParam("id", opt => opt.MapFrom(s => s.Id))
                .ForCtorParam("name", opt => opt.MapFrom(s => s.Name));
        }
    }

    public class MixedCtorAndPropertyProfile : Profile
    {
        public MixedCtorAndPropertyProfile()
        {
            CreateMap<SourceMixed, DestMixed>()
                .ForCtorParam("id", opt => opt.MapFrom(s => s.Id))
                .ForCtorParam("name", opt => opt.MapFrom(s => s.Name))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description));
        }
    }

    public class OptionalCtorParamProfile : Profile
    {
        public OptionalCtorParamProfile()
        {
            CreateMap<SourceWithOptional, DestWithOptionalParam>()
                .ForCtorParam("title", opt => opt.MapFrom(s => s.Title));
            // Note: count has default value, so we don't map it
        }
    }

    public class TransformationCtorParamProfile : Profile
    {
        public TransformationCtorParamProfile()
        {
            CreateMap<PersonSource, PersonDest>()
                .ForCtorParam("id", opt => opt.MapFrom(s => s.PersonId * 2))
                .ForCtorParam("name", opt => opt.MapFrom(s => s.FullName.ToUpper()));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void ForCtorParam_RenamedParameters_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RenamedCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new PersonSource { PersonId = 42, FullName = "John Doe", Age = 30 };

        // Act
        var dest = mapper.Map<PersonDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Id);
        Assert.Equal("John Doe", dest.Name);
        Assert.Equal(30, dest.Age);
    }

    [Fact]
    public void ForCtorParam_MultipleParameters_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleCtorParamsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMultipleProps { Value1 = 10, Value2 = 20, Text = "Hello" };

        // Act
        var dest = mapper.Map<DestWithThreeCtorParams>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.A);
        Assert.Equal(20, dest.B);
        Assert.Equal("Hello", dest.C);
    }

    [Fact]
    public void ForCtorParam_SamePropertyNames_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SameNameCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceSameNames { Id = 123, Name = "Test" };

        // Act
        var dest = mapper.Map<DestSameNames>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(123, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void ForCtorParam_MixedWithForMember_BothWork()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedCtorAndPropertyProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceMixed { Id = 1, Name = "Product", Description = "A great product" };

        // Act
        var dest = mapper.Map<DestMixed>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(1, dest.Id);
        Assert.Equal("Product", dest.Name);
        Assert.Equal("A great product", dest.Description);
    }

    [Fact]
    public void ForCtorParam_OptionalParameter_UsesDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OptionalCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithOptional { Title = "My Title" };

        // Act
        var dest = mapper.Map<DestWithOptionalParam>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("My Title", dest.Title);
        Assert.Equal(10, dest.Count); // Default value
    }

    [Fact]
    public void ForCtorParam_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RenamedCtorParamProfile>());
        var mapper = config.CreateMapper();
        PersonSource? nullSource = null;

        // Act
        var dest = mapper.Map<PersonDest>(nullSource!);

        // Assert
        Assert.Null(dest);
    }

    [Fact]
    public void ForCtorParam_WithTransformation_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TransformationCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new PersonSource { PersonId = 5, FullName = "test" };

        // Act
        var dest = mapper.Map<PersonDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.Id); // 5 * 2
        Assert.Equal("TEST", dest.Name); // ToUpper
    }

    #endregion
}
