using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 BeforeMap/AfterMap lifecycle hooks.
/// AutoMapper API compatibility: CreateMap<S, D>().BeforeMap((s, d) => ...).AfterMap((s, d) => ...)
/// </summary>
public class BeforeAfterMapTests
{
    #region Test Models

    public class LifecycleSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class LifecycleDestination
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? MappedAt { get; set; }
        public string? MappedBy { get; set; }
        public bool WasValidated { get; set; }
    }

    public class TrackingSource
    {
        public string? Value { get; set; }
    }

    public class TrackingDestination
    {
        public string? Value { get; set; }
        public List<string> Logs { get; } = new();
    }

    #endregion

    #region Profiles

    public class BeforeMapProfile : Profile
    {
        public BeforeMapProfile()
        {
            CreateMap<LifecycleSource, LifecycleDestination>()
                .BeforeMap((src, dest) => dest.MappedAt = DateTime.UtcNow);
        }
    }

    public class AfterMapProfile : Profile
    {
        public AfterMapProfile()
        {
            CreateMap<LifecycleSource, LifecycleDestination>()
                .AfterMap((src, dest) => dest.WasValidated = true);
        }
    }

    public class BeforeAndAfterMapProfile : Profile
    {
        public BeforeAndAfterMapProfile()
        {
            CreateMap<LifecycleSource, LifecycleDestination>()
                .BeforeMap((src, dest) => dest.MappedBy = "BeforeMap")
                .AfterMap((src, dest) => dest.WasValidated = true);
        }
    }

    public class BeforeMapWithContextProfile : Profile
    {
        public BeforeMapWithContextProfile()
        {
            CreateMap<LifecycleSource, LifecycleDestination>()
                .BeforeMap((src, dest, ctx) =>
                {
                    dest.MappedBy = "WithContext";
                    dest.MappedAt = DateTime.UtcNow;
                });
        }
    }

    public class AfterMapWithContextProfile : Profile
    {
        public AfterMapWithContextProfile()
        {
            CreateMap<LifecycleSource, LifecycleDestination>()
                .AfterMap((src, dest, ctx) =>
                {
                    dest.WasValidated = true;
                    dest.MappedBy = "AfterMapContext";
                });
        }
    }

    public class ExecutionOrderTrackingProfile : Profile
    {
        public ExecutionOrderTrackingProfile()
        {
            CreateMap<TrackingSource, TrackingDestination>()
                .BeforeMap((src, dest) => dest.Logs.Add("BeforeMap"))
                .AfterMap((src, dest) => dest.Logs.Add("AfterMap"));
        }
    }

    public class AfterMapDefaultsProfile : Profile
    {
        public AfterMapDefaultsProfile()
        {
            CreateMap<LifecycleSource, LifecycleDestination>()
                .AfterMap((src, dest) =>
                {
                    // AfterMap can set defaults after property mapping completes
                    if (dest.CreatedAt == null)
                    {
                        dest.CreatedAt = DateTime.MinValue;
                    }
                });
        }
    }

    public class ChainedBeforeMapProfile : Profile
    {
        public ChainedBeforeMapProfile()
        {
            CreateMap<TrackingSource, TrackingDestination>()
                .BeforeMap((src, dest) => dest.Logs.Add("First"))
                .BeforeMap((src, dest) => dest.Logs.Add("Second"));
        }
    }

    public class ChainedAfterMapProfile : Profile
    {
        public ChainedAfterMapProfile()
        {
            CreateMap<TrackingSource, TrackingDestination>()
                .AfterMap((src, dest) => dest.Logs.Add("First"))
                .AfterMap((src, dest) => dest.Logs.Add("Second"));
        }
    }

    #endregion

    [Fact]
    public void BeforeMap_ExecutesBeforePropertyMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BeforeMapProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1, Name = "Test" };

        // Act
        var beforeMapping = DateTime.UtcNow;
        var result = mapper.Map<LifecycleDestination>(source);
        var afterMapping = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.NotNull(result.MappedAt);
        Assert.True(result.MappedAt >= beforeMapping && result.MappedAt <= afterMapping);
    }

    [Fact]
    public void AfterMap_ExecutesAfterPropertyMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AfterMapProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1, Name = "Test" };

        // Act
        var result = mapper.Map<LifecycleDestination>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
        Assert.True(result.WasValidated);
    }

    [Fact]
    public void BeforeAndAfterMap_BothExecute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BeforeAndAfterMapProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1 };

        // Act
        var result = mapper.Map<LifecycleDestination>(source);

        // Assert
        Assert.Equal("BeforeMap", result.MappedBy);
        Assert.True(result.WasValidated);
    }

    [Fact]
    public void BeforeMap_WithResolutionContext_HasAccess()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BeforeMapWithContextProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1 };

        // Act
        var result = mapper.Map<LifecycleDestination>(source);

        // Assert
        Assert.Equal("WithContext", result.MappedBy);
        Assert.NotNull(result.MappedAt);
    }

    [Fact]
    public void AfterMap_WithResolutionContext_HasAccess()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AfterMapWithContextProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1 };

        // Act
        var result = mapper.Map<LifecycleDestination>(source);

        // Assert
        Assert.True(result.WasValidated);
        Assert.Equal("AfterMapContext", result.MappedBy);
    }

    [Fact]
    public void BeforeAndAfterMap_ExecuteInCorrectOrder()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExecutionOrderTrackingProfile>());
        var mapper = config.CreateMapper();
        var source = new TrackingSource { Value = "Test" };

        // Act
        var result = mapper.Map<TrackingDestination>(source);

        // Assert
        Assert.Equal(2, result.Logs.Count);
        Assert.Equal("BeforeMap", result.Logs[0]);
        Assert.Equal("AfterMap", result.Logs[1]);
        Assert.Equal("Test", result.Value);
    }

    [Fact]
    public void AfterMap_CanSetDefaultsOnDestination()
    {
        // AfterMap can set defaults after property mapping - useful for null handling
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AfterMapDefaultsProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1, Name = "Test", CreatedAt = null };

        // Act
        var result = mapper.Map<LifecycleDestination>(source);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(DateTime.MinValue, result.CreatedAt);
    }

    [Fact]
    public void BeforeMap_ChainedCalls_LastWins()
    {
        // Note: AutoMapper behavior - last BeforeMap replaces previous ones
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ChainedBeforeMapProfile>());
        var mapper = config.CreateMapper();
        var source = new TrackingSource { Value = "Test" };

        // Act
        var result = mapper.Map<TrackingDestination>(source);

        // Assert - Only the last BeforeMap should execute
        Assert.Single(result.Logs);
        Assert.Equal("Second", result.Logs[0]);
    }

    [Fact]
    public void AfterMap_ChainedCalls_LastWins()
    {
        // Note: AutoMapper behavior - last AfterMap replaces previous ones
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ChainedAfterMapProfile>());
        var mapper = config.CreateMapper();
        var source = new TrackingSource { Value = "Test" };

        // Act
        var result = mapper.Map<TrackingDestination>(source);

        // Assert - Only the last AfterMap should execute
        Assert.Single(result.Logs);
        Assert.Equal("Second", result.Logs[0]);
    }

    [Fact]
    public void MapToExisting_BeforeAndAfterMapExecute()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BeforeAndAfterMapProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Id = 1, Name = "Test" };
        var destination = new LifecycleDestination();

        // Act
        mapper.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
        Assert.Equal("BeforeMap", destination.MappedBy);
        Assert.True(destination.WasValidated);
    }

    [Fact]
    public void BeforeMap_WithNullSource_DoesNotThrow()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BeforeMapProfile>());
        var mapper = config.CreateMapper();
        LifecycleSource? nullSource = null;

        // Act & Assert - null source should return default
        var result = mapper.Map<LifecycleDestination>(nullSource!);
        Assert.Null(result);
    }
}
