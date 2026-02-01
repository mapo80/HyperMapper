using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 MaxDepth() feature - circular reference depth limiting.
/// AutoMapper API compatibility: CreateMap<S, D>().MaxDepth(3)
/// </summary>
public class MaxDepthTests
{
    #region Test Models

    public class TreeNode
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public TreeNode? Parent { get; set; }
        public List<TreeNode> Children { get; set; } = new();
    }

    public class TreeNodeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public TreeNodeDto? Parent { get; set; }
        public List<TreeNodeDto> Children { get; set; } = new();
    }

    public class LinkedListNode
    {
        public int Value { get; set; }
        public LinkedListNode? Next { get; set; }
    }

    public class LinkedListNodeDto
    {
        public int Value { get; set; }
        public LinkedListNodeDto? Next { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Employee? Manager { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public EmployeeDto? Manager { get; set; }
    }

    #endregion

    #region Profiles

    public class MaxDepthOneProfile : Profile
    {
        public MaxDepthOneProfile()
        {
            CreateMap<TreeNode, TreeNodeDto>().MaxDepth(1);
        }
    }

    public class MaxDepthTwoProfile : Profile
    {
        public MaxDepthTwoProfile()
        {
            CreateMap<TreeNode, TreeNodeDto>().MaxDepth(2);
        }
    }

    public class MaxDepthThreeProfile : Profile
    {
        public MaxDepthThreeProfile()
        {
            CreateMap<TreeNode, TreeNodeDto>().MaxDepth(3);
        }
    }

    public class LinkedListMaxDepthProfile : Profile
    {
        public LinkedListMaxDepthProfile()
        {
            CreateMap<LinkedListNode, LinkedListNodeDto>().MaxDepth(3);
        }
    }

    public class NoMaxDepthProfile : Profile
    {
        public NoMaxDepthProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }

    public class ManagerMaxDepthProfile : Profile
    {
        public ManagerMaxDepthProfile()
        {
            CreateMap<Employee, EmployeeDto>().MaxDepth(2);
        }
    }

    #endregion

    [Fact]
    public void MaxDepth_AtLimit_StopsMapping()
    {
        // Arrange - MaxDepth(1) means only one level of TreeNode->TreeNodeDto is allowed
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaxDepthOneProfile>());
        var mapper = config.CreateMapper();

        var grandchild = new TreeNode { Id = 3, Name = "Grandchild" };
        var child = new TreeNode { Id = 2, Name = "Child", Children = new List<TreeNode> { grandchild } };
        var root = new TreeNode { Id = 1, Name = "Root", Children = new List<TreeNode> { child } };
        grandchild.Parent = child;
        child.Parent = root;

        // Act
        var result = mapper.Map<TreeNodeDto>(root);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Root", result.Name);
        // At depth 1: root is mapped (depth 1 consumed), children collection elements are null (depth exceeded)
        Assert.Single(result.Children);
        Assert.Null(result.Children[0]); // Depth exceeded for child elements
    }

    [Fact]
    public void MaxDepth_BelowLimit_ContinuesMapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaxDepthTwoProfile>());
        var mapper = config.CreateMapper();

        var child = new TreeNode { Id = 2, Name = "Child" };
        var root = new TreeNode { Id = 1, Name = "Root", Children = new List<TreeNode> { child } };

        // Act
        var result = mapper.Map<TreeNodeDto>(root);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Single(result.Children);
        Assert.Equal(2, result.Children[0].Id);
        Assert.Equal("Child", result.Children[0].Name);
    }

    [Fact]
    public void MaxDepth_LinkedList_StopsAtDepth()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinkedListMaxDepthProfile>());
        var mapper = config.CreateMapper();

        // Create a linked list: 1 -> 2 -> 3 -> 4 -> 5
        var node5 = new LinkedListNode { Value = 5 };
        var node4 = new LinkedListNode { Value = 4, Next = node5 };
        var node3 = new LinkedListNode { Value = 3, Next = node4 };
        var node2 = new LinkedListNode { Value = 2, Next = node3 };
        var node1 = new LinkedListNode { Value = 1, Next = node2 };

        // Act
        var result = mapper.Map<LinkedListNodeDto>(node1);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.NotNull(result.Next);
        Assert.Equal(2, result.Next.Value);
        Assert.NotNull(result.Next.Next);
        Assert.Equal(3, result.Next.Next.Value);
        // At depth 3, the 4th node should be null
        Assert.Null(result.Next.Next.Next);
    }

    [Fact]
    public void MaxDepth_ZeroDepth_MapsOnlyRoot()
    {
        // Arrange - MaxDepth(1) means only root level
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaxDepthOneProfile>());
        var mapper = config.CreateMapper();

        var child = new TreeNode { Id = 2, Name = "Child" };
        var root = new TreeNode { Id = 1, Name = "Root", Children = new List<TreeNode> { child } };

        // Act
        var result = mapper.Map<TreeNodeDto>(root);

        // Assert
        Assert.Equal(1, result.Id);
        // Children collection has elements but they are null (depth exceeded)
        Assert.Single(result.Children);
        Assert.Null(result.Children[0]); // Child is null because depth exceeded
    }

    [Fact]
    public void MaxDepth_NotSet_UnlimitedDepth()
    {
        // Arrange - no MaxDepth configured (but shallow graph to avoid stack overflow)
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NoMaxDepthProfile>());
        var mapper = config.CreateMapper();

        var ceo = new Employee { Id = 1, Name = "CEO" };
        var vp = new Employee { Id = 2, Name = "VP", Manager = ceo };
        var manager = new Employee { Id = 3, Name = "Manager", Manager = vp };
        var employee = new Employee { Id = 4, Name = "Employee", Manager = manager };

        // Act
        var result = mapper.Map<EmployeeDto>(employee);

        // Assert - all levels mapped (no depth limit)
        Assert.Equal(4, result.Id);
        Assert.NotNull(result.Manager);
        Assert.Equal(3, result.Manager.Id);
        Assert.NotNull(result.Manager.Manager);
        Assert.Equal(2, result.Manager.Manager.Id);
        Assert.NotNull(result.Manager.Manager.Manager);
        Assert.Equal(1, result.Manager.Manager.Manager.Id);
    }

    [Fact]
    public void MaxDepth_PerTypeTracking_WorksCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ManagerMaxDepthProfile>());
        var mapper = config.CreateMapper();

        // Three levels deep
        var ceo = new Employee { Id = 1, Name = "CEO" };
        var vp = new Employee { Id = 2, Name = "VP", Manager = ceo };
        var employee = new Employee { Id = 3, Name = "Employee", Manager = vp };

        // Act
        var result = mapper.Map<EmployeeDto>(employee);

        // Assert - depth 2 means: employee (1) -> manager/vp (2) -> stops
        Assert.Equal(3, result.Id);
        Assert.NotNull(result.Manager);
        Assert.Equal(2, result.Manager.Id);
        Assert.Null(result.Manager.Manager); // Depth limit reached
    }

    [Fact]
    public void MaxDepth_WithCollections_TracksDepthPerElement()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaxDepthThreeProfile>());
        var mapper = config.CreateMapper();

        var grandchild1 = new TreeNode { Id = 4, Name = "GC1" };
        var grandchild2 = new TreeNode { Id = 5, Name = "GC2" };
        var child1 = new TreeNode { Id = 2, Name = "C1", Children = new List<TreeNode> { grandchild1 } };
        var child2 = new TreeNode { Id = 3, Name = "C2", Children = new List<TreeNode> { grandchild2 } };
        var root = new TreeNode { Id = 1, Name = "Root", Children = new List<TreeNode> { child1, child2 } };

        // Act
        var result = mapper.Map<TreeNodeDto>(root);

        // Assert - depth 3 allows root -> child -> grandchild
        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.Children.Count);
        Assert.Equal(2, result.Children[0].Id);
        Assert.Single(result.Children[0].Children);
        Assert.Equal(4, result.Children[0].Children[0].Id);
    }
}
