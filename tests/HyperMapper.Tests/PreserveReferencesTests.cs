using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v8.0.0 PreserveReferences() feature - object reference preservation.
/// AutoMapper API compatibility: CreateMap<S, D>().PreserveReferences()
/// </summary>
public class PreserveReferencesTests
{
    #region Test Models

    public class Person
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Person? Spouse { get; set; }
        public Person? BestFriend { get; set; }
    }

    public class PersonDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public PersonDto? Spouse { get; set; }
        public PersonDto? BestFriend { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Employee? Manager { get; set; }
        public List<Employee> DirectReports { get; set; } = new();
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public EmployeeDto? Manager { get; set; }
        public List<EmployeeDto> DirectReports { get; set; } = new();
    }

    public class Node
    {
        public int Id { get; set; }
        public Node? Next { get; set; }
        public Node? Previous { get; set; }
    }

    public class NodeDto
    {
        public int Id { get; set; }
        public NodeDto? Next { get; set; }
        public NodeDto? Previous { get; set; }
    }

    #endregion

    #region Profiles

    public class PreserveRefsProfile : Profile
    {
        public PreserveRefsProfile()
        {
            CreateMap<Person, PersonDto>().PreserveReferences();
        }
    }

    public class NoPreserveRefsProfile : Profile
    {
        public NoPreserveRefsProfile()
        {
            CreateMap<Person, PersonDto>();
        }
    }

    public class EmployeePreserveRefsProfile : Profile
    {
        public EmployeePreserveRefsProfile()
        {
            CreateMap<Employee, EmployeeDto>().PreserveReferences();
        }
    }

    public class NodePreserveRefsProfile : Profile
    {
        public NodePreserveRefsProfile()
        {
            CreateMap<Node, NodeDto>().PreserveReferences();
        }
    }

    #endregion

    [Fact]
    public void PreserveReferences_SameObjectTwice_ReturnsSameDestination()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreserveRefsProfile>());
        var mapper = config.CreateMapper();

        var sharedFriend = new Person { Id = 3, Name = "Shared Friend" };
        var alice = new Person { Id = 1, Name = "Alice", BestFriend = sharedFriend };
        var bob = new Person { Id = 2, Name = "Bob", BestFriend = sharedFriend };

        // Both Alice and Bob have the same best friend
        alice.Spouse = bob;
        bob.Spouse = alice;

        // Act
        var aliceDto = mapper.Map<PersonDto>(alice);

        // Assert
        Assert.Equal(1, aliceDto.Id);
        Assert.NotNull(aliceDto.BestFriend);
        Assert.Equal(3, aliceDto.BestFriend.Id);

        Assert.NotNull(aliceDto.Spouse);
        Assert.Equal(2, aliceDto.Spouse.Id);

        // Both should reference the same shared friend DTO instance
        Assert.NotNull(aliceDto.Spouse.BestFriend);
        Assert.Same(aliceDto.BestFriend, aliceDto.Spouse.BestFriend);
    }

    [Fact]
    public void PreserveReferences_CircularReference_HandlesSafely()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreserveRefsProfile>());
        var mapper = config.CreateMapper();

        // Create circular reference: Alice <-> Bob
        var alice = new Person { Id = 1, Name = "Alice" };
        var bob = new Person { Id = 2, Name = "Bob" };
        alice.Spouse = bob;
        bob.Spouse = alice;

        // Act - should not stack overflow
        var aliceDto = mapper.Map<PersonDto>(alice);

        // Assert
        Assert.Equal(1, aliceDto.Id);
        Assert.NotNull(aliceDto.Spouse);
        Assert.Equal(2, aliceDto.Spouse.Id);
        // Bob's spouse should be the same instance as aliceDto
        Assert.Same(aliceDto, aliceDto.Spouse.Spouse);
    }

    [Fact]
    public void PreserveReferences_NotEnabled_CreatesNewInstances()
    {
        // Arrange - without PreserveReferences
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NoPreserveRefsProfile>());
        var mapper = config.CreateMapper();

        var sharedFriend = new Person { Id = 3, Name = "Shared" };
        var alice = new Person { Id = 1, Name = "Alice", BestFriend = sharedFriend };

        // Act
        var aliceDto = mapper.Map<PersonDto>(alice);

        // Assert
        Assert.NotNull(aliceDto.BestFriend);
        Assert.Equal(3, aliceDto.BestFriend.Id);
    }

    [Fact]
    public void PreserveReferences_InCollection_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EmployeePreserveRefsProfile>());
        var mapper = config.CreateMapper();

        var manager = new Employee { Id = 1, Name = "Manager" };
        var emp1 = new Employee { Id = 2, Name = "Emp1", Manager = manager };
        var emp2 = new Employee { Id = 3, Name = "Emp2", Manager = manager };

        manager.DirectReports.Add(emp1);
        manager.DirectReports.Add(emp2);

        // Act
        var managerDto = mapper.Map<EmployeeDto>(manager);

        // Assert
        Assert.Equal(2, managerDto.DirectReports.Count);

        // Both employees should have the same manager reference
        Assert.NotNull(managerDto.DirectReports[0].Manager);
        Assert.NotNull(managerDto.DirectReports[1].Manager);
        Assert.Same(managerDto, managerDto.DirectReports[0].Manager);
        Assert.Same(managerDto, managerDto.DirectReports[1].Manager);
    }

    [Fact]
    public void PreserveReferences_DoublyLinkedList_BothDirections()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NodePreserveRefsProfile>());
        var mapper = config.CreateMapper();

        var node1 = new Node { Id = 1 };
        var node2 = new Node { Id = 2 };
        var node3 = new Node { Id = 3 };

        node1.Next = node2;
        node2.Previous = node1;
        node2.Next = node3;
        node3.Previous = node2;

        // Act
        var result = mapper.Map<NodeDto>(node1);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Next);
        Assert.Equal(2, result.Next.Id);

        // Check backward reference
        Assert.Same(result, result.Next.Previous);

        Assert.NotNull(result.Next.Next);
        Assert.Equal(3, result.Next.Next.Id);
        Assert.Same(result.Next, result.Next.Next.Previous);
    }

    [Fact]
    public void PreserveReferences_SelfReference_HandlesSafely()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreserveRefsProfile>());
        var mapper = config.CreateMapper();

        // Person with self-reference as best friend
        var narcissist = new Person { Id = 1, Name = "Narcissist" };
        narcissist.BestFriend = narcissist;

        // Act
        var result = mapper.Map<PersonDto>(narcissist);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.BestFriend);
        Assert.Same(result, result.BestFriend); // Self-reference preserved
    }

    [Fact]
    public void PreserveReferences_DeepGraph_PreservesAll()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EmployeePreserveRefsProfile>());
        var mapper = config.CreateMapper();

        // Create a diamond reference pattern
        // CEO -> VP1, VP2 -> Manager (shared)
        var ceo = new Employee { Id = 1, Name = "CEO" };
        var vp1 = new Employee { Id = 2, Name = "VP1", Manager = ceo };
        var vp2 = new Employee { Id = 3, Name = "VP2", Manager = ceo };
        var sharedManager = new Employee { Id = 4, Name = "Shared", Manager = vp1 };

        vp1.DirectReports.Add(sharedManager);
        vp2.DirectReports.Add(sharedManager); // Same manager under both VPs

        ceo.DirectReports.Add(vp1);
        ceo.DirectReports.Add(vp2);

        // Act
        var result = mapper.Map<EmployeeDto>(ceo);

        // Assert
        Assert.Equal(2, result.DirectReports.Count);
        var vp1Dto = result.DirectReports[0];
        var vp2Dto = result.DirectReports[1];

        // Both VPs should report to the same CEO
        Assert.Same(result, vp1Dto.Manager);
        Assert.Same(result, vp2Dto.Manager);

        // Both VPs have the shared manager in their direct reports
        Assert.Single(vp1Dto.DirectReports);
        Assert.Single(vp2Dto.DirectReports);

        // The shared manager should be the same instance
        Assert.Same(vp1Dto.DirectReports[0], vp2Dto.DirectReports[0]);
    }
}
