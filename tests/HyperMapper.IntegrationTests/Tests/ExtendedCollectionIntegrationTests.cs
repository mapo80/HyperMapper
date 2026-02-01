using System.Collections.ObjectModel;
using Xunit;

namespace HyperMapper.IntegrationTests.Tests;

/// <summary>
/// v7.1.0: Integration tests for extended collection types in Source Generator.
/// These tests verify that the Source Generator produces correct compile-time code
/// for Dictionary, ObservableCollection, LinkedList, Queue, Stack, SortedSet, ISet, IReadOnlySet.
/// </summary>
public class ExtendedCollectionIntegrationTests
{
    #region Test Types

    public class Item
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ItemDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    // Dictionary types
    public class SourceWithDict
    {
        public Dictionary<string, int>? Scores { get; set; }
    }

    public class DestWithDict
    {
        public Dictionary<string, int>? Scores { get; set; }
    }

    public class SourceWithDictComplex
    {
        public Dictionary<int, Item>? Items { get; set; }
    }

    public class DestWithDictComplex
    {
        public Dictionary<int, ItemDto>? Items { get; set; }
    }

    public class SourceWithIDict
    {
        public IDictionary<string, string>? Data { get; set; }
    }

    public class DestFromIDict
    {
        public Dictionary<string, string>? Data { get; set; }
    }

    // ObservableCollection types
    public class SourceWithObservable
    {
        public ObservableCollection<string>? Tags { get; set; }
    }

    public class DestWithObservable
    {
        public ObservableCollection<string>? Tags { get; set; }
    }

    public class SourceWithObservableComplex
    {
        public ObservableCollection<Item>? Items { get; set; }
    }

    public class DestWithObservableComplex
    {
        public ObservableCollection<ItemDto>? Items { get; set; }
    }

    // LinkedList types
    public class SourceWithLinkedList
    {
        public LinkedList<string>? Nodes { get; set; }
    }

    public class DestWithLinkedList
    {
        public LinkedList<string>? Nodes { get; set; }
    }

    // Queue types
    public class SourceWithQueue
    {
        public Queue<string>? Tasks { get; set; }
    }

    public class DestWithQueue
    {
        public Queue<string>? Tasks { get; set; }
    }

    // Stack types
    public class SourceWithStack
    {
        public Stack<string>? History { get; set; }
    }

    public class DestWithStack
    {
        public Stack<string>? History { get; set; }
    }

    // SortedSet types
    public class SourceWithSortedSet
    {
        public SortedSet<int>? Numbers { get; set; }
    }

    public class DestWithSortedSet
    {
        public SortedSet<int>? Numbers { get; set; }
    }

    // ISet types
    public class SourceWithISet
    {
        public ISet<string>? Tags { get; set; }
    }

    public class DestFromISet
    {
        public HashSet<string>? Tags { get; set; }
    }

    // IReadOnlySet types
    public class SourceWithReadOnlySet
    {
        public IReadOnlySet<string>? Tags { get; set; }
    }

    public class DestFromReadOnlySet
    {
        public HashSet<string>? Tags { get; set; }
    }

    // All collections in one class
    public class SourceAllCollections
    {
        public Dictionary<string, int>? Dict { get; set; }
        public ObservableCollection<string>? Observable { get; set; }
        public LinkedList<string>? LinkedList { get; set; }
        public Queue<string>? Queue { get; set; }
        public Stack<int>? Stack { get; set; }
        public SortedSet<string>? SortedSet { get; set; }
        public List<string>? List { get; set; }
        public HashSet<int>? HashSet { get; set; }
    }

    public class DestAllCollections
    {
        public Dictionary<string, int>? Dict { get; set; }
        public ObservableCollection<string>? Observable { get; set; }
        public LinkedList<string>? LinkedList { get; set; }
        public Queue<string>? Queue { get; set; }
        public Stack<int>? Stack { get; set; }
        public SortedSet<string>? SortedSet { get; set; }
        public List<string>? List { get; set; }
        public HashSet<int>? HashSet { get; set; }
    }

    #endregion

    #region Test Profiles

    public class DictProfile : Profile
    {
        public DictProfile()
        {
            CreateMap<SourceWithDict, DestWithDict>();
        }
    }

    public class DictComplexProfile : Profile
    {
        public DictComplexProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithDictComplex, DestWithDictComplex>();
        }
    }

    public class IDictProfile : Profile
    {
        public IDictProfile()
        {
            CreateMap<SourceWithIDict, DestFromIDict>();
        }
    }

    public class ObservableProfile : Profile
    {
        public ObservableProfile()
        {
            CreateMap<SourceWithObservable, DestWithObservable>();
        }
    }

    public class ObservableComplexProfile : Profile
    {
        public ObservableComplexProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithObservableComplex, DestWithObservableComplex>();
        }
    }

    public class LinkedListProfile : Profile
    {
        public LinkedListProfile()
        {
            CreateMap<SourceWithLinkedList, DestWithLinkedList>();
        }
    }

    public class QueueProfile : Profile
    {
        public QueueProfile()
        {
            CreateMap<SourceWithQueue, DestWithQueue>();
        }
    }

    public class StackProfile : Profile
    {
        public StackProfile()
        {
            CreateMap<SourceWithStack, DestWithStack>();
        }
    }

    public class SortedSetProfile : Profile
    {
        public SortedSetProfile()
        {
            CreateMap<SourceWithSortedSet, DestWithSortedSet>();
        }
    }

    public class ISetProfile : Profile
    {
        public ISetProfile()
        {
            CreateMap<SourceWithISet, DestFromISet>();
        }
    }

    public class IReadOnlySetProfile : Profile
    {
        public IReadOnlySetProfile()
        {
            CreateMap<SourceWithReadOnlySet, DestFromReadOnlySet>();
        }
    }

    public class AllCollectionsProfile : Profile
    {
        public AllCollectionsProfile()
        {
            CreateMap<SourceAllCollections, DestAllCollections>();
        }
    }

    #endregion

    #region Dictionary Integration Tests (3 tests)

    [Fact]
    public void Integration_Dictionary_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDict
        {
            Scores = new Dictionary<string, int>
            {
                ["Alice"] = 95,
                ["Bob"] = 87,
                ["Charlie"] = 92
            }
        };

        // Act
        var dest = mapper.Map<DestWithDict>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Scores);
        Assert.Equal(3, dest.Scores.Count);
        Assert.Equal(95, dest.Scores["Alice"]);
        Assert.Equal(87, dest.Scores["Bob"]);
        Assert.Equal(92, dest.Scores["Charlie"]);
    }

    [Fact]
    public void Integration_Dictionary_WithComplexValues_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictComplexProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictComplex
        {
            Items = new Dictionary<int, Item>
            {
                [1] = new Item { Id = 1, Name = "First" },
                [2] = new Item { Id = 2, Name = "Second" },
                [3] = new Item { Id = 3, Name = "Third" }
            }
        };

        // Act
        var dest = mapper.Map<DestWithDictComplex>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(3, dest.Items.Count);
        Assert.Equal("First", dest.Items[1].Name);
        Assert.Equal("Second", dest.Items[2].Name);
        Assert.Equal("Third", dest.Items[3].Name);
    }

    [Fact]
    public void Integration_IDictionary_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IDictProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithIDict
        {
            Data = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        var dest = mapper.Map<DestFromIDict>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Data);
        Assert.Equal(2, dest.Data.Count);
        Assert.Equal("value1", dest.Data["key1"]);
    }

    #endregion

    #region ObservableCollection Integration Tests (2 tests)

    [Fact]
    public void Integration_ObservableCollection_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ObservableProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithObservable
        {
            Tags = new ObservableCollection<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        var dest = mapper.Map<DestWithObservable>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.IsType<ObservableCollection<string>>(dest.Tags);
        Assert.Equal(3, dest.Tags.Count);
        Assert.Equal("tag1", dest.Tags[0]);
        Assert.Equal("tag2", dest.Tags[1]);
        Assert.Equal("tag3", dest.Tags[2]);
    }

    [Fact]
    public void Integration_ObservableCollection_WithNestedTypes_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ObservableComplexProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithObservableComplex
        {
            Items = new ObservableCollection<Item>
            {
                new Item { Id = 1, Name = "A" },
                new Item { Id = 2, Name = "B" }
            }
        };

        // Act
        var dest = mapper.Map<DestWithObservableComplex>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.IsType<ObservableCollection<ItemDto>>(dest.Items);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("A", dest.Items[0].Name);
        Assert.Equal("B", dest.Items[1].Name);
    }

    #endregion

    #region LinkedList Integration Tests (2 tests)

    [Fact]
    public void Integration_LinkedList_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinkedListProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithLinkedList
        {
            Nodes = new LinkedList<string>(new[] { "A", "B", "C" })
        };

        // Act
        var dest = mapper.Map<DestWithLinkedList>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Nodes);
        Assert.IsType<LinkedList<string>>(dest.Nodes);
        Assert.Equal(3, dest.Nodes.Count);
    }

    [Fact]
    public void Integration_LinkedList_OrderPreserved()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinkedListProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithLinkedList
        {
            Nodes = new LinkedList<string>(new[] { "first", "second", "third" })
        };

        // Act
        var dest = mapper.Map<DestWithLinkedList>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Nodes);
        var arr = dest.Nodes.ToArray();
        Assert.Equal("first", arr[0]);
        Assert.Equal("second", arr[1]);
        Assert.Equal("third", arr[2]);
    }

    #endregion

    #region Queue Integration Tests (2 tests)

    [Fact]
    public void Integration_Queue_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<QueueProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithQueue
        {
            Tasks = new Queue<string>(new[] { "task1", "task2", "task3" })
        };

        // Act
        var dest = mapper.Map<DestWithQueue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tasks);
        Assert.IsType<Queue<string>>(dest.Tasks);
        Assert.Equal(3, dest.Tasks.Count);
    }

    [Fact]
    public void Integration_Queue_FIFOPreserved()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<QueueProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithQueue
        {
            Tasks = new Queue<string>(new[] { "first", "second", "third" })
        };

        // Act
        var dest = mapper.Map<DestWithQueue>(source);

        // Assert - FIFO: first in, first out
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tasks);
        Assert.Equal("first", dest.Tasks.Dequeue());
        Assert.Equal("second", dest.Tasks.Dequeue());
        Assert.Equal("third", dest.Tasks.Dequeue());
    }

    #endregion

    #region Stack Integration Tests (2 tests)

    [Fact]
    public void Integration_Stack_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StackProfile>());
        var mapper = config.CreateMapper();

        var sourceStack = new Stack<string>();
        sourceStack.Push("bottom");
        sourceStack.Push("middle");
        sourceStack.Push("top");

        var source = new SourceWithStack { History = sourceStack };

        // Act
        var dest = mapper.Map<DestWithStack>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.History);
        Assert.IsType<Stack<string>>(dest.History);
        Assert.Equal(3, dest.History.Count);
    }

    [Fact]
    public void Integration_Stack_LIFOPreserved()
    {
        // Arrange - Critical test for Stack order preservation
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StackProfile>());
        var mapper = config.CreateMapper();

        var sourceStack = new Stack<string>();
        sourceStack.Push("first");  // bottom
        sourceStack.Push("second");
        sourceStack.Push("third");  // top

        var source = new SourceWithStack { History = sourceStack };

        // Act
        var dest = mapper.Map<DestWithStack>(source);

        // Assert - LIFO: last in, first out (third should be on top)
        Assert.NotNull(dest);
        Assert.NotNull(dest.History);
        Assert.Equal("third", dest.History.Pop());   // top
        Assert.Equal("second", dest.History.Pop());
        Assert.Equal("first", dest.History.Pop());   // bottom
    }

    #endregion

    #region SortedSet Integration Tests (2 tests)

    [Fact]
    public void Integration_SortedSet_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SortedSetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithSortedSet
        {
            Numbers = new SortedSet<int> { 50, 10, 30, 20, 40 }
        };

        // Act
        var dest = mapper.Map<DestWithSortedSet>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Numbers);
        Assert.IsType<SortedSet<int>>(dest.Numbers);
        Assert.Equal(5, dest.Numbers.Count);
    }

    [Fact]
    public void Integration_SortedSet_SortOrderPreserved()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SortedSetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithSortedSet
        {
            Numbers = new SortedSet<int> { 50, 10, 30, 20, 40 }
        };

        // Act
        var dest = mapper.Map<DestWithSortedSet>(source);

        // Assert - SortedSet maintains sorted order
        Assert.NotNull(dest);
        Assert.NotNull(dest.Numbers);
        var arr = dest.Numbers.ToArray();
        Assert.Equal(10, arr[0]);
        Assert.Equal(20, arr[1]);
        Assert.Equal(30, arr[2]);
        Assert.Equal(40, arr[3]);
        Assert.Equal(50, arr[4]);
    }

    #endregion

    #region ISet/IReadOnlySet Integration Tests (2 tests)

    [Fact]
    public void Integration_ISet_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ISetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithISet
        {
            Tags = new HashSet<string> { "a", "b", "c" }
        };

        // Act
        var dest = mapper.Map<DestFromISet>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.IsType<HashSet<string>>(dest.Tags);
        Assert.Equal(3, dest.Tags.Count);
        Assert.Contains("a", dest.Tags);
        Assert.Contains("b", dest.Tags);
        Assert.Contains("c", dest.Tags);
    }

    [Fact]
    public void Integration_IReadOnlySet_GeneratedCodeWorks()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IReadOnlySetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithReadOnlySet
        {
            Tags = new HashSet<string> { "x", "y", "z" }
        };

        // Act
        var dest = mapper.Map<DestFromReadOnlySet>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.IsType<HashSet<string>>(dest.Tags);
        Assert.Equal(3, dest.Tags.Count);
        Assert.Contains("x", dest.Tags);
    }

    #endregion

    #region Complex Scenarios Integration Tests (3 tests)

    [Fact]
    public void Integration_AllCollectionTypes_SingleProfile_Works()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AllCollectionsProfile>());
        var mapper = config.CreateMapper();

        var stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var source = new SourceAllCollections
        {
            Dict = new Dictionary<string, int> { ["key"] = 42 },
            Observable = new ObservableCollection<string> { "obs1", "obs2" },
            LinkedList = new LinkedList<string>(new[] { "ll1", "ll2" }),
            Queue = new Queue<string>(new[] { "q1", "q2" }),
            Stack = stack,
            SortedSet = new SortedSet<string> { "z", "a", "m" },
            List = new List<string> { "list1", "list2" },
            HashSet = new HashSet<int> { 1, 2, 3 }
        };

        // Act
        var dest = mapper.Map<DestAllCollections>(source);

        // Assert
        Assert.NotNull(dest);

        Assert.NotNull(dest.Dict);
        Assert.Single(dest.Dict);
        Assert.Equal(42, dest.Dict["key"]);

        Assert.NotNull(dest.Observable);
        Assert.Equal(2, dest.Observable.Count);

        Assert.NotNull(dest.LinkedList);
        Assert.Equal(2, dest.LinkedList.Count);

        Assert.NotNull(dest.Queue);
        Assert.Equal(2, dest.Queue.Count);

        Assert.NotNull(dest.Stack);
        Assert.Equal(3, dest.Stack.Count);
        Assert.Equal(3, dest.Stack.Pop()); // top

        Assert.NotNull(dest.SortedSet);
        Assert.Equal(3, dest.SortedSet.Count);
        Assert.Equal("a", dest.SortedSet.First()); // sorted

        Assert.NotNull(dest.List);
        Assert.Equal(2, dest.List.Count);

        Assert.NotNull(dest.HashSet);
        Assert.Equal(3, dest.HashSet.Count);
    }

    [Fact]
    public void Integration_MixedExistingAndNewCollections_Works()
    {
        // Test that v7.0 collections (List, HashSet) still work alongside v7.1.0 collections
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AllCollectionsProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceAllCollections
        {
            List = new List<string> { "a", "b", "c" },
            HashSet = new HashSet<int> { 10, 20, 30 },
            Queue = new Queue<string>(new[] { "q1" }),
            Stack = new Stack<int>(new[] { 100 })
        };

        // Act
        var dest = mapper.Map<DestAllCollections>(source);

        // Assert - v7.0 collections
        Assert.NotNull(dest.List);
        Assert.Equal(3, dest.List.Count);
        Assert.Equal("a", dest.List[0]);

        Assert.NotNull(dest.HashSet);
        Assert.Equal(3, dest.HashSet.Count);

        // Assert - v7.1.0 collections
        Assert.NotNull(dest.Queue);
        Assert.Single(dest.Queue);

        Assert.NotNull(dest.Stack);
        Assert.Single(dest.Stack);
    }

    [Fact]
    public void Integration_CollectionWithCustomConverters_Works()
    {
        // Arrange - uses mapping with element transformation
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictComplexProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictComplex
        {
            Items = new Dictionary<int, Item>
            {
                [100] = new Item { Id = 100, Name = "ItemA" },
                [200] = new Item { Id = 200, Name = "ItemB" }
            }
        };

        // Act
        var dest = mapper.Map<DestWithDictComplex>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("ItemA", dest.Items[100].Name);
        Assert.Equal("ItemB", dest.Items[200].Name);
    }

    #endregion
}
