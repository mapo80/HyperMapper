using System.Reflection;
using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 Assembly scanning - Automatic Profile discovery.
/// AutoMapper API compatibility: cfg.AddMaps(typeof(MyProfile).Assembly)
/// </summary>
public class AssemblyScanningTests
{
    #region Test Models

    public class ScanSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ScanDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    #endregion

    #region Test Profiles

    public class ScanTestProfile : Profile
    {
        public ScanTestProfile()
        {
            CreateMap<ScanSource, ScanDest>();
        }
    }

    #endregion

    [Fact]
    public void AddMaps_Assembly_DiscoversProfiles()
    {
        // Arrange - Scan this test assembly
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AssemblyScanningTests).Assembly));
        var mapper = config.CreateMapper();
        var source = new ScanSource { Id = 42, Name = "Scanned" };

        // Act
        var result = mapper.Map<ScanDest>(source);

        // Assert - ScanTestProfile should have been discovered
        Assert.Equal(42, result.Id);
        Assert.Equal("Scanned", result.Name);
    }

    [Fact]
    public void AddMaps_MultipleAssemblies_DiscoversAll()
    {
        // Arrange - Scan both HyperMapper and test assembly
        var assembly1 = typeof(MapperConfiguration).Assembly;
        var assembly2 = typeof(AssemblyScanningTests).Assembly;

        var config = new MapperConfiguration(cfg => cfg.AddMaps(assembly1, assembly2));
        var mapper = config.CreateMapper();
        var source = new ScanSource { Id = 1, Name = "Multi" };

        // Act
        var result = mapper.Map<ScanDest>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Multi", result.Name);
    }

    [Fact]
    public void AddMaps_NoProfiles_NoError()
    {
        // Arrange - Scan an assembly with no profiles (mscorlib has none)
        var assemblyWithNoProfiles = typeof(object).Assembly;

        // Act & Assert - Should not throw
        var config = new MapperConfiguration(cfg => cfg.AddMaps(assemblyWithNoProfiles));
        Assert.NotNull(config);
    }

    [Fact]
    public void AddMaps_MarkerTypes_Works()
    {
        // Arrange - Use marker types instead of assemblies directly
        var config = new MapperConfiguration(cfg =>
            cfg.AddMaps(new[] { typeof(AssemblyScanningTests) }));
        var mapper = config.CreateMapper();
        var source = new ScanSource { Id = 99, Name = "Marker" };

        // Act
        var result = mapper.Map<ScanDest>(source);

        // Assert
        Assert.Equal(99, result.Id);
        Assert.Equal("Marker", result.Name);
    }

    [Fact]
    public void AddMaps_CombinedWithAddProfile_Works()
    {
        // Arrange - Mix AddMaps with explicit AddProfile
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(AssemblyScanningTests).Assembly);
            // Adding explicit profile should work alongside scanning
        });
        var mapper = config.CreateMapper();
        var source = new ScanSource { Id = 123, Name = "Combined" };

        // Act
        var result = mapper.Map<ScanDest>(source);

        // Assert
        Assert.Equal(123, result.Id);
    }
}
