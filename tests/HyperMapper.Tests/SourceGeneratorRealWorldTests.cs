using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v11.0.1: Real-world scenario tests for Source Generator fixes.
/// Tests critical production issues: nested navigation properties, collection type conversion, and using directives.
/// </summary>
public class SourceGeneratorRealWorldTests
{
    #region Test Models (Simulated EF Entities and DTOs)

    // Entity models (simulating database entities with navigation properties)
    public class Sede
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
    }

    public class Area
    {
        public int Id { get; set; }
        public Sede? IdSedeNavigation { get; set; }
    }

    public class Postazione
    {
        public int Id { get; set; }
        public Area? IdAreaNavigation { get; set; }
    }

    public class Prenotazione
    {
        public int Id { get; set; }
        public Postazione? IdPostazioneNavigation { get; set; }
    }

    // DTO models
    public class SedeDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
    }

    public class AreaDto
    {
        public int Id { get; set; }
        public SedeDto? Sede { get; set; }
    }

    public class PostazioneDto
    {
        public int Id { get; set; }
        public AreaDto? Area { get; set; }
    }

    public class PrenotazioneDto
    {
        public int Id { get; set; }
        public SedeDto? Sede { get; set; }
        public AreaDto? Area { get; set; }
        public PostazioneDto? Postazione { get; set; }
    }

    // Collection models
    public class PrenotazioneMassivaDettaglio
    {
        public int Id { get; set; }
        public string Note { get; set; } = "";
    }

    public class PrenotazioneMassiva
    {
        public int Id { get; set; }
        public ICollection<PrenotazioneMassivaDettaglio> Dettagli { get; set; } = new List<PrenotazioneMassivaDettaglio>();
    }

    public class PrenotazioneMassivaDettaglioDto
    {
        public int Id { get; set; }
        public string Note { get; set; } = "";
    }

    public class PrenotazioneMassivaDto
    {
        public int Id { get; set; }
        public ICollection<PrenotazioneMassivaDettaglioDto> Dettagli { get; set; } = new List<PrenotazioneMassivaDettaglioDto>();
    }

    // Geometry models for ConvertUsing
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Geometry
    {
        public Point Coordinate { get; set; } = new Point();
    }

    public class GeometryPointDto
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    // Nullable types
    public class SourceWithNullableInt
    {
        public int? NullableCount { get; set; }
        public ICollection<string>? Tags { get; set; }
    }

    public class DestWithNonNullableInt
    {
        public int Count { get; set; }
        public ICollection<string> Tags { get; set; } = new List<string>();
    }

    #endregion

    #region Test Profiles

    public class RealWorldProfile : Profile
    {
        public RealWorldProfile()
        {
            // Basic entity mappings
            CreateMap<Sede, SedeDto>();
            CreateMap<Area, AreaDto>()
                .ForMember(d => d.Sede, opt => opt.MapFrom(s => s.IdSedeNavigation));
            CreateMap<Postazione, PostazioneDto>()
                .ForMember(d => d.Area, opt => opt.MapFrom(s => s.IdAreaNavigation));

            // TEST PROBLEM #3: Nested navigation properties
            CreateMap<Prenotazione, PrenotazioneDto>()
                .ForMember(dest => dest.Postazione, opt => opt.MapFrom(src => src.IdPostazioneNavigation))
                .ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.IdPostazioneNavigation.IdAreaNavigation))
                .ForMember(dest => dest.Sede, opt => opt.MapFrom(src => src.IdPostazioneNavigation.IdAreaNavigation.IdSedeNavigation));

            // TEST PROBLEM #6: Collection type conversion
            CreateMap<PrenotazioneMassivaDettaglio, PrenotazioneMassivaDettaglioDto>();
            CreateMap<PrenotazioneMassiva, PrenotazioneMassivaDto>()
                .ForMember(dest => dest.Dettagli, opt => opt.MapFrom(src => src.Dettagli));

            // TEST PROBLEM #7: Nullable value types
            CreateMap<SourceWithNullableInt, DestWithNonNullableInt>()
                .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.NullableCount ?? 0))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags ?? new List<string>()));
        }
    }

    public class ConvertUsingProfile : Profile
    {
        public ConvertUsingProfile()
        {
            // TEST PROBLEM #1: ConvertUsing with lambda
            CreateMap<Geometry, GeometryPointDto>()
                .ConvertUsing(s => new GeometryPointDto { X = s.Coordinate.X, Y = s.Coordinate.Y });
        }
    }

    #endregion

    #region Tests for Problem #3: Nested Navigation Properties

    [Fact]
    public void NestedNavigationProperties_ShouldCallMappers()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new Prenotazione
        {
            Id = 1,
            IdPostazioneNavigation = new Postazione
            {
                Id = 10,
                IdAreaNavigation = new Area
                {
                    Id = 100,
                    IdSedeNavigation = new Sede { Id = 1000, Nome = "Sede Principale" }
                }
            }
        };

        // Act
        var result = mapper.Map<Prenotazione, PrenotazioneDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Sede);
        Assert.Equal(1000, result.Sede.Id);
        Assert.Equal("Sede Principale", result.Sede.Nome);
        Assert.NotNull(result.Area);
        Assert.Equal(100, result.Area.Id);
        Assert.NotNull(result.Postazione);
        Assert.Equal(10, result.Postazione.Id);
    }

    [Fact]
    public void NestedNavigationProperties_WithNulls_ShouldReturnNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new Prenotazione
        {
            Id = 1,
            IdPostazioneNavigation = null  // Null navigation
        };

        // Act
        var result = mapper.Map<Prenotazione, PrenotazioneDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Sede);
        Assert.Null(result.Area);
        Assert.Null(result.Postazione);
    }

    [Fact]
    public void NestedNavigationProperties_PartialNull_ShouldHandleGracefully()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new Prenotazione
        {
            Id = 1,
            IdPostazioneNavigation = new Postazione
            {
                Id = 10,
                IdAreaNavigation = null  // Partial null chain
            }
        };

        // Act
        var result = mapper.Map<Prenotazione, PrenotazioneDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Sede);
        Assert.Null(result.Area);
        Assert.NotNull(result.Postazione);
        Assert.Equal(10, result.Postazione.Id);
    }

    #endregion

    #region Tests for Problem #6: Collection Type Conversion

    [Fact]
    public void CollectionTypeConversion_ShouldMapElements()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new PrenotazioneMassiva
        {
            Id = 1,
            Dettagli = new List<PrenotazioneMassivaDettaglio>
            {
                new PrenotazioneMassivaDettaglio { Id = 1, Note = "Note 1" },
                new PrenotazioneMassivaDettaglio { Id = 2, Note = "Note 2" }
            }
        };

        // Act
        var result = mapper.Map<PrenotazioneMassiva, PrenotazioneMassivaDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dettagli);
        Assert.Equal(2, result.Dettagli.Count);
        Assert.Equal("Note 1", result.Dettagli.First().Note);
        Assert.Equal("Note 2", result.Dettagli.Last().Note);
    }

    [Fact]
    public void CollectionTypeConversion_EmptyCollection_ShouldReturnEmpty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new PrenotazioneMassiva
        {
            Id = 1,
            Dettagli = new List<PrenotazioneMassivaDettaglio>()
        };

        // Act
        var result = mapper.Map<PrenotazioneMassiva, PrenotazioneMassivaDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Dettagli);
        Assert.Empty(result.Dettagli);
    }

    #endregion

    #region Tests for Problem #7: Nullable Value Types

    [Fact]
    public void NullableValueType_ToNonNullable_ShouldUseDefault()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableInt
        {
            NullableCount = null,
            Tags = null
        };

        // Act
        var result = mapper.Map<SourceWithNullableInt, DestWithNonNullableInt>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);  // default(int) = 0
        Assert.NotNull(result.Tags);
    }

    [Fact]
    public void NullableValueType_WithValue_ShouldMap()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RealWorldProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new SourceWithNullableInt
        {
            NullableCount = 42,
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var result = mapper.Map<SourceWithNullableInt, DestWithNonNullableInt>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Count);
        Assert.Equal(2, result.Tags.Count);
    }

    #endregion

    #region Tests for Problem #1: ConvertUsing with Lambda

    [Fact]
    public void ConvertUsing_WithLambda_ShouldInline()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConvertUsingProfile>());
        // Note: Source generator will generate code at compile-time if enabled
        var mapper = config.CreateMapper();

        var source = new Geometry
        {
            Coordinate = new Point { X = 10.5, Y = 20.3 }
        };

        // Act
        var result = mapper.Map<Geometry, GeometryPointDto>(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10.5, result.X);
        Assert.Equal(20.3, result.Y);
    }

    #endregion
}
