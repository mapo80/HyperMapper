using Xunit;

namespace HyperMapper.Tests;

public class BasicMappingTests
{
    [Fact]
    public void Map_SimpleObject_MapsAllProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { Id = 1, Name = "Test" };

        // Act
        var dest = mapper.Map<Destination>(source);

        // Assert
        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Map_WithExplicitTypes_MapsAllProperties()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { Id = 1, Name = "Test" };

        var dest = mapper.Map<Source, Destination>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    public void Map_WithForMember_UsesCustomMapping()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { Id = 1, Name = "Test", Description = "Desc" };

        var dest = mapper.Map<DestinationCustom>(source);

        Assert.Equal("Test - Desc", dest.FullName);
    }

    [Fact]
    public void Map_WithIgnore_DoesNotMapProperty()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { Id = 1, Name = "Test" };

        var dest = mapper.Map<DestinationWithIgnored>(source);

        Assert.Equal(1, dest.Id);
        Assert.Null(dest.Name);
    }

    [Fact]
    public void Map_Collection_MapsAllItems()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();
        var sources = new List<Source>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        var dests = mapper.Map<IEnumerable<Destination>>(sources);

        Assert.Equal(2, dests.Count());
        Assert.Equal("One", dests.First().Name);
        Assert.Equal("Two", dests.Last().Name);
    }

    [Fact]
    public void Map_List_MapsAllItems()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();
        var sources = new List<Source>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        var dests = mapper.Map<List<Destination>>(sources);

        Assert.Equal(2, dests.Count);
    }

    [Fact]
    public void ReverseMap_WorksBidirectionally()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();

        var source = new Source { Id = 1, Name = "Test" };
        var dest = mapper.Map<Destination>(source);
        var backToSource = mapper.Map<Source>(dest);

        Assert.Equal(source.Id, backToSource.Id);
        Assert.Equal(source.Name, backToSource.Name);
    }

    [Fact]
    public void Map_ToExisting_UpdatesDestination()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();

        var source = new Source { Id = 1, Name = "Updated" };
        var dest = new Destination { Id = 99, Name = "Original" };

        mapper.Map(source, dest);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Updated", dest.Name);
    }

    [Fact]
    public void Map_NullSource_ReturnsDefault()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();

        Source? source = null;
        var dest = mapper.Map<Destination>(source!);

        Assert.Null(dest);
    }

    [Fact]
    public void Map_WithPreCondition_AppliesWhenTrue()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCondition { Id = 1, ConditionalValue = "HasValue", ShouldMap = true };
        var dest = mapper.Map<DestWithCondition>(source);

        Assert.Equal("HasValue", dest.ConditionalValue);
    }

    [Fact]
    public void Map_WithPreCondition_SkipsWhenFalse()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithCondition { Id = 1, ConditionalValue = "HasValue", ShouldMap = false };
        var dest = mapper.Map<DestWithCondition>(source);

        Assert.Null(dest.ConditionalValue);
    }
}

// Test classes
public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DestinationCustom
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class DestinationWithIgnored
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class SourceWithCondition
{
    public int Id { get; set; }
    public string ConditionalValue { get; set; } = string.Empty;
    public bool ShouldMap { get; set; }
}

public class DestWithCondition
{
    public int Id { get; set; }
    public string? ConditionalValue { get; set; }
}

public class TestProfile : Profile
{
    public TestProfile()
    {
        CreateMap<Source, Destination>().ReverseMap();

        CreateMap<Source, DestinationCustom>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.Name} - {s.Description}"));

        CreateMap<Source, DestinationWithIgnored>()
            .ForMember(d => d.Name, opt => opt.Ignore());

        CreateMap<SourceWithCondition, DestWithCondition>()
            .ForMember(d => d.ConditionalValue, opt =>
            {
                opt.PreCondition(s => s.ShouldMap);
                opt.MapFrom(s => s.ConditionalValue);
            });
    }
}
