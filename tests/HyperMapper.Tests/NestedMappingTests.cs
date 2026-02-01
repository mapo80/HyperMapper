using Xunit;

namespace HyperMapper.Tests;

public class NestedMappingTests
{
    [Fact]
    public void Map_NestedObjects_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedProfile>());
        var mapper = config.CreateMapper();

        var source = new Parent
        {
            Id = 1,
            Name = "Parent",
            Child = new Child { Id = 2, Name = "Child" }
        };

        var dest = mapper.Map<ParentDto>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Parent", dest.Name);
        Assert.NotNull(dest.Child);
        Assert.Equal(2, dest.Child.Id);
        Assert.Equal("Child", dest.Child.Name);
    }

    [Fact]
    public void Map_NestedCollection_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedProfile>());
        var mapper = config.CreateMapper();

        var source = new ParentWithChildren
        {
            Id = 1,
            Name = "Parent",
            Children = new List<Child>
            {
                new() { Id = 2, Name = "Child1" },
                new() { Id = 3, Name = "Child2" }
            }
        };

        var dest = mapper.Map<ParentWithChildrenDto>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal(2, dest.Children.Count);
        Assert.Equal("Child1", dest.Children[0].Name);
        Assert.Equal("Child2", dest.Children[1].Name);
    }

    [Fact]
    public void Map_DeepNesting_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedProfile>());
        var mapper = config.CreateMapper();

        var source = new Level1
        {
            Value = "L1",
            Level2 = new Level2
            {
                Value = "L2",
                Level3 = new Level3 { Value = "L3" }
            }
        };

        var dest = mapper.Map<Level1Dto>(source);

        Assert.Equal("L1", dest.Value);
        Assert.NotNull(dest.Level2);
        Assert.Equal("L2", dest.Level2.Value);
        Assert.NotNull(dest.Level2.Level3);
        Assert.Equal("L3", dest.Level2.Level3.Value);
    }
}

public class NestedProfile : Profile
{
    public NestedProfile()
    {
        CreateMap<Child, ChildDto>();
        CreateMap<Parent, ParentDto>();
        CreateMap<ParentWithChildren, ParentWithChildrenDto>();

        CreateMap<Level3, Level3Dto>();
        CreateMap<Level2, Level2Dto>();
        CreateMap<Level1, Level1Dto>();
    }
}

// Test classes
public class Child
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ChildDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Parent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Child? Child { get; set; }
}

public class ParentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChildDto? Child { get; set; }
}

public class ParentWithChildren
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Child> Children { get; set; } = new();
}

public class ParentWithChildrenDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ChildDto> Children { get; set; } = new();
}

public class Level3
{
    public string Value { get; set; } = string.Empty;
}

public class Level3Dto
{
    public string Value { get; set; } = string.Empty;
}

public class Level2
{
    public string Value { get; set; } = string.Empty;
    public Level3? Level3 { get; set; }
}

public class Level2Dto
{
    public string Value { get; set; } = string.Empty;
    public Level3Dto? Level3 { get; set; }
}

public class Level1
{
    public string Value { get; set; } = string.Empty;
    public Level2? Level2 { get; set; }
}

public class Level1Dto
{
    public string Value { get; set; } = string.Empty;
    public Level2Dto? Level2 { get; set; }
}
