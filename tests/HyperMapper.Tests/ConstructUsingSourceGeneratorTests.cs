using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v9.0.0: Unit tests for ConstructUsing() in Source Generator.
/// Tests custom constructor lambda at compile-time code generation.
/// </summary>
public class ConstructUsingSourceGeneratorTests
{
    #region Test Types

    public class SimpleSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class SimpleDest
    {
        public int Id { get; }
        public string Name { get; }

        public SimpleDest(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class SourceWithValues
    {
        public int Value { get; set; }
        public string Text { get; set; } = "";
    }

    public class DestWithObjectInit
    {
        public int DoubledValue { get; set; }
        public string UpperText { get; set; } = "";
    }

    public class SourceForImmutable
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Label { get; set; } = "";
    }

    public record ImmutableDest(int X, int Y, string Label);

    public class SourceWithDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }

    public class DestWithDate
    {
        public DateTime Date { get; }

        public DestWithDate(DateTime date)
        {
            Date = date;
        }
    }

    public class SourceWithComputed
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class DestWithComputed
    {
        public string FullName { get; }

        public DestWithComputed(string fullName)
        {
            FullName = fullName;
        }
    }

    #endregion

    #region Test Profiles

    public class BasicConstructUsingProfile : Profile
    {
        public BasicConstructUsingProfile()
        {
            CreateMap<SimpleSource, SimpleDest>()
                .ConstructUsing(s => new SimpleDest(s.Id, s.Name));
        }
    }

    public class ConstructUsingWithObjectInitProfile : Profile
    {
        public ConstructUsingWithObjectInitProfile()
        {
            CreateMap<SourceWithValues, DestWithObjectInit>()
                .ConstructUsing(s => new DestWithObjectInit
                {
                    DoubledValue = s.Value * 2,
                    UpperText = s.Text.ToUpper()
                });
        }
    }

    public class ConstructUsingWithArithmeticProfile : Profile
    {
        public ConstructUsingWithArithmeticProfile()
        {
            CreateMap<SimpleSource, SimpleDest>()
                .ConstructUsing(s => new SimpleDest(s.Id * 10, s.Name + "_suffix"));
        }
    }

    public class ConstructUsingImmutableProfile : Profile
    {
        public ConstructUsingImmutableProfile()
        {
            CreateMap<SourceForImmutable, ImmutableDest>()
                .ConstructUsing(s => new ImmutableDest(s.X, s.Y, s.Label));
        }
    }

    public class ConstructUsingWithDateProfile : Profile
    {
        public ConstructUsingWithDateProfile()
        {
            CreateMap<SourceWithDate, DestWithDate>()
                .ConstructUsing(s => new DestWithDate(new DateTime(s.Year, s.Month, s.Day)));
        }
    }

    public class ConstructUsingComputedProfile : Profile
    {
        public ConstructUsingComputedProfile()
        {
            CreateMap<SourceWithComputed, DestWithComputed>()
                .ConstructUsing(s => new DestWithComputed(s.FirstName + " " + s.LastName));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void ConstructUsing_BasicLambda_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicConstructUsingProfile>());
        var mapper = config.CreateMapper();
        var source = new SimpleSource { Id = 42, Name = "Test" };

        // Act
        var dest = mapper.Map<SimpleDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(42, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void ConstructUsing_ObjectInitializer_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingWithObjectInitProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithValues { Value = 5, Text = "hello" };

        // Act
        var dest = mapper.Map<DestWithObjectInit>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.DoubledValue); // 5 * 2
        Assert.Equal("HELLO", dest.UpperText); // ToUpper
    }

    [Fact]
    public void ConstructUsing_WithArithmetic_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingWithArithmeticProfile>());
        var mapper = config.CreateMapper();
        var source = new SimpleSource { Id = 3, Name = "test" };

        // Act
        var dest = mapper.Map<SimpleDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(30, dest.Id); // 3 * 10
        Assert.Equal("test_suffix", dest.Name);
    }

    [Fact]
    public void ConstructUsing_ImmutableType_AllPropertiesSet()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingImmutableProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForImmutable { X = 10, Y = 20, Label = "Point" };

        // Act
        var dest = mapper.Map<ImmutableDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(10, dest.X);
        Assert.Equal(20, dest.Y);
        Assert.Equal("Point", dest.Label);
    }

    [Fact]
    public void ConstructUsing_WithDateConstruction_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingWithDateProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDate { Year = 2024, Month = 6, Day = 15 };

        // Act
        var dest = mapper.Map<DestWithDate>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(new DateTime(2024, 6, 15), dest.Date);
    }

    [Fact]
    public void ConstructUsing_ComputedProperty_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingComputedProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithComputed { FirstName = "John", LastName = "Doe" };

        // Act
        var dest = mapper.Map<DestWithComputed>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void ConstructUsing_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicConstructUsingProfile>());
        var mapper = config.CreateMapper();
        SimpleSource? nullSource = null;

        // Act
        var dest = mapper.Map<SimpleDest>(nullSource!);

        // Assert
        Assert.Null(dest);
    }

    #endregion
}
