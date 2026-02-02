using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

/// <summary>
/// v12.0.2: Tests for Source Generator handling of types from external assemblies.
/// Uses EntityFrameworkCore types as external type examples (no Hangfire dependency needed).
/// This tests the fix for namespace resolution issues with types from external assemblies.
/// </summary>
public class ExternalNamespaceSourceGeneratorTests
{
    #region Test Models

    // Tipo che usa enum da assembly esterno (Microsoft.EntityFrameworkCore)
    public class EntityStateSource
    {
        public EntityState CurrentState { get; set; }
        public string Name { get; set; } = "";
    }

    public class EntityStateDto
    {
        public string StateDescription { get; set; } = "";
        public EntityState State { get; set; }
        public string Name { get; set; } = "";
    }

    // Tipo con proprietà complessa da assembly esterno
    public class DbContextOptionsWrapper
    {
        public DbContextOptions<TestDbContext> Options { get; set; } = null!;
        public string ConnectionName { get; set; } = "";
    }

    public class DbContextOptionsDto
    {
        public bool HasOptions { get; set; }
        public string ConnectionName { get; set; } = "";
    }

    // DbContext minimale per il test
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    #endregion

    #region Profile

    public class ExternalNamespaceProfile : Profile
    {
        public ExternalNamespaceProfile()
        {
            // Mapping che preserva il tipo enum esterno
            CreateMap<EntityStateSource, EntityStateDto>()
                .ForMember(d => d.StateDescription, opt => opt.MapFrom(s => s.CurrentState.ToString()))
                .ForMember(d => d.State, opt => opt.MapFrom(s => s.CurrentState));

            // Mapping con tipo generico esterno
            CreateMap<DbContextOptionsWrapper, DbContextOptionsDto>()
                .ForMember(d => d.HasOptions, opt => opt.MapFrom(s => s.Options != null));
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void CodeGen_ExternalEnum_ShouldMapCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExternalNamespaceProfile>());
        var mapper = config.CreateMapper();

        var source = new EntityStateSource
        {
            CurrentState = EntityState.Modified,
            Name = "Test Entity"
        };

        // Act
        var result = mapper.Map<EntityStateDto>(source);

        // Assert
        Assert.Equal(EntityState.Modified, result.State);
        Assert.Equal("Modified", result.StateDescription);
        Assert.Equal("Test Entity", result.Name);
    }

    [Fact]
    public void CodeGen_AllEntityStates_ShouldMapCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExternalNamespaceProfile>());
        var mapper = config.CreateMapper();

        var states = new[]
        {
            EntityState.Detached,
            EntityState.Unchanged,
            EntityState.Added,
            EntityState.Deleted,
            EntityState.Modified
        };

        foreach (var state in states)
        {
            var source = new EntityStateSource { CurrentState = state, Name = state.ToString() };

            // Act
            var result = mapper.Map<EntityStateDto>(source);

            // Assert
            Assert.Equal(state, result.State);
            Assert.Equal(state.ToString(), result.StateDescription);
        }
    }

    [Fact]
    public void CodeGen_ExternalGenericType_ShouldMapCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExternalNamespaceProfile>());
        var mapper = config.CreateMapper();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;

        var source = new DbContextOptionsWrapper
        {
            Options = options,
            ConnectionName = "TestConnection"
        };

        // Act
        var result = mapper.Map<DbContextOptionsDto>(source);

        // Assert
        Assert.True(result.HasOptions);
        Assert.Equal("TestConnection", result.ConnectionName);
    }

    [Fact]
    public void CodeGen_ExternalType_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExternalNamespaceProfile>());
        var mapper = config.CreateMapper();

        EntityStateSource? source = null;

        // Act
        var result = mapper.Map<EntityStateDto?>(source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CodeGen_ExternalType_Collection_ShouldMapAllItems()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExternalNamespaceProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<EntityStateSource>
        {
            new() { CurrentState = EntityState.Added, Name = "Item1" },
            new() { CurrentState = EntityState.Modified, Name = "Item2" },
            new() { CurrentState = EntityState.Deleted, Name = "Item3" }
        };

        // Act
        var results = mapper.Map<List<EntityStateDto>>(sources);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(EntityState.Added, results[0].State);
        Assert.Equal(EntityState.Modified, results[1].State);
        Assert.Equal(EntityState.Deleted, results[2].State);
    }

    #endregion
}
