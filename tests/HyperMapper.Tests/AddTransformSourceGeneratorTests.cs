using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v10.0.0: Unit tests for AddTransform&lt;T&gt;() in Source Generator.
/// Tests type-level transformations applied to all properties of a given type.
/// </summary>
public class AddTransformSourceGeneratorTests
{
    #region Test Types

    public class Person
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? MiddleName { get; set; }
        public int Age { get; set; }
    }

    public class PersonDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? MiddleName { get; set; }
        public int Age { get; set; }
    }

    public class Product
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class ProductDto
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class Document
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public decimal Value { get; set; }
    }

    public class DocumentDto
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public decimal Value { get; set; }
    }

    #endregion

    #region Test Profiles

    public class StringTrimTransformProfile : Profile
    {
        public StringTrimTransformProfile()
        {
            CreateMap<Person, PersonDto>()
                .AddTransform<string>(s => s == null ? s! : s.Trim());
        }
    }

    public class StringUpperTransformProfile : Profile
    {
        public StringUpperTransformProfile()
        {
            CreateMap<Person, PersonDto>()
                .AddTransform<string>(s => s == null ? s! : s.ToUpper());
        }
    }

    public class DecimalRoundTransformProfile : Profile
    {
        public DecimalRoundTransformProfile()
        {
            CreateMap<Product, ProductDto>()
                .AddTransform<decimal>(d => Math.Round(d, 2));
        }
    }

    public class MultipleTransformsProfile : Profile
    {
        public MultipleTransformsProfile()
        {
            CreateMap<Document, DocumentDto>()
                .AddTransform<string>(s => s == null ? s! : s.Trim())
                .AddTransform<decimal>(d => Math.Round(d, 2));
        }
    }

    public class StringEmptyToNullTransformProfile : Profile
    {
        public StringEmptyToNullTransformProfile()
        {
            CreateMap<Person, PersonDto>()
                .AddTransform<string>(s => string.IsNullOrEmpty(s) ? null! : s);
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void AddTransform_StringTrim_TrimsAllStringProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringTrimTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            FirstName = "  John  ",
            LastName = "  Doe  ",
            MiddleName = "  William  ",
            Age = 30
        };

        // Act
        var dest = mapper.Map<PersonDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.FirstName);
        Assert.Equal("Doe", dest.LastName);
        Assert.Equal("William", dest.MiddleName);
        Assert.Equal(30, dest.Age);  // Non-string not affected
    }

    [Fact]
    public void AddTransform_StringUpper_UppercasesAllStringProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringUpperTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            FirstName = "john",
            LastName = "doe",
            MiddleName = "william",
            Age = 25
        };

        // Act
        var dest = mapper.Map<PersonDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("JOHN", dest.FirstName);
        Assert.Equal("DOE", dest.LastName);
        Assert.Equal("WILLIAM", dest.MiddleName);
    }

    [Fact]
    public void AddTransform_DecimalRound_RoundsAllDecimalProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DecimalRoundTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new Product
        {
            Name = "Widget",
            Description = "A great widget",
            Price = 19.999m
        };

        // Act
        var dest = mapper.Map<ProductDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Widget", dest.Name);  // String not affected
        Assert.Equal("A great widget", dest.Description);
        Assert.Equal(20.00m, dest.Price);
    }

    [Fact]
    public void AddTransform_MultipleTransforms_AllApply()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MultipleTransformsProfile>());
        var mapper = config.CreateMapper();
        var source = new Document
        {
            Title = "  My Document  ",
            Content = "  Important content  ",
            Value = 123.456m
        };

        // Act
        var dest = mapper.Map<DocumentDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("My Document", dest.Title);  // Trimmed
        Assert.Equal("Important content", dest.Content);  // Trimmed
        Assert.Equal(123.46m, dest.Value);  // Rounded
    }

    [Fact]
    public void AddTransform_NullValue_HandledGracefully()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringTrimTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            MiddleName = null,  // Null string
            Age = 30
        };

        // Act
        var dest = mapper.Map<PersonDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.FirstName);
        Assert.Null(dest.MiddleName);  // Null passed through
    }

    [Fact]
    public void AddTransform_StringEmptyToNull_ConvertsEmptyStrings()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringEmptyToNullTransformProfile>());
        var mapper = config.CreateMapper();
        var source = new Person
        {
            FirstName = "John",
            LastName = "",  // Empty string
            MiddleName = "",  // Empty string
            Age = 30
        };

        // Act
        var dest = mapper.Map<PersonDto>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.FirstName);
        Assert.Null(dest.LastName);  // Empty converted to null
        Assert.Null(dest.MiddleName);  // Empty converted to null
    }

    [Fact]
    public void AddTransform_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StringTrimTransformProfile>());
        var mapper = config.CreateMapper();
        Person? nullPerson = null;

        // Act
        var dest = mapper.Map<PersonDto>(nullPerson!);

        // Assert
        Assert.Null(dest);
    }

    #endregion
}
