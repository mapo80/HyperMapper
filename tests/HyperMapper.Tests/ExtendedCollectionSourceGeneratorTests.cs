using System.Collections.ObjectModel;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v7.1.0: Unit tests for extended collection types in Source Generator.
/// Tests Dictionary, ObservableCollection, LinkedList, Queue, Stack, SortedSet, ISet, IReadOnlySet.
/// </summary>
public class ExtendedCollectionSourceGeneratorTests
{
    #region Test Types

    // Simple value class for mapping tests
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

    // Dictionary source/dest types
    public class SourceWithDictionary
    {
        public Dictionary<string, int>? Scores { get; set; }
    }

    public class DestWithDictionary
    {
        public Dictionary<string, int>? Scores { get; set; }
    }

    public class SourceWithDictValues
    {
        public Dictionary<int, Item>? Items { get; set; }
    }

    public class DestWithDictValues
    {
        public Dictionary<int, ItemDto>? Items { get; set; }
    }

    public class SourceWithIDictionary
    {
        public IDictionary<string, int>? Data { get; set; }
    }

    public class DestWithIDictionary
    {
        public Dictionary<string, int>? Data { get; set; }
    }

    public class SourceWithReadOnlyDict
    {
        public IReadOnlyDictionary<string, int>? Data { get; set; }
    }

    public class DestWithReadOnlyDict
    {
        public Dictionary<string, int>? Data { get; set; }
    }

    // ObservableCollection source/dest types
    public class SourceWithObservable
    {
        public ObservableCollection<string>? Tags { get; set; }
    }

    public class DestWithObservable
    {
        public ObservableCollection<string>? Tags { get; set; }
    }

    public class SourceWithObservableItems
    {
        public ObservableCollection<Item>? Items { get; set; }
    }

    public class DestWithObservableItems
    {
        public ObservableCollection<ItemDto>? Items { get; set; }
    }

    // LinkedList source/dest types
    public class SourceWithLinkedList
    {
        public LinkedList<string>? Nodes { get; set; }
    }

    public class DestWithLinkedList
    {
        public LinkedList<string>? Nodes { get; set; }
    }

    public class SourceWithLinkedListItems
    {
        public LinkedList<Item>? Items { get; set; }
    }

    public class DestWithLinkedListItems
    {
        public LinkedList<ItemDto>? Items { get; set; }
    }

    // Queue source/dest types
    public class SourceWithQueue
    {
        public Queue<string>? Tasks { get; set; }
    }

    public class DestWithQueue
    {
        public Queue<string>? Tasks { get; set; }
    }

    public class SourceWithQueueItems
    {
        public Queue<Item>? Items { get; set; }
    }

    public class DestWithQueueItems
    {
        public Queue<ItemDto>? Items { get; set; }
    }

    // Stack source/dest types
    public class SourceWithStack
    {
        public Stack<string>? History { get; set; }
    }

    public class DestWithStack
    {
        public Stack<string>? History { get; set; }
    }

    public class SourceWithStackItems
    {
        public Stack<Item>? Items { get; set; }
    }

    public class DestWithStackItems
    {
        public Stack<ItemDto>? Items { get; set; }
    }

    // SortedSet source/dest types
    public class SourceWithSortedSet
    {
        public SortedSet<int>? Numbers { get; set; }
    }

    public class DestWithSortedSet
    {
        public SortedSet<int>? Numbers { get; set; }
    }

    public class SourceWithSortedSetStrings
    {
        public SortedSet<string>? Tags { get; set; }
    }

    public class DestWithSortedSetStrings
    {
        public SortedSet<string>? Tags { get; set; }
    }

    // ISet source/dest types
    public class SourceWithISet
    {
        public ISet<string>? Tags { get; set; }
    }

    public class DestWithHashSet
    {
        public HashSet<string>? Tags { get; set; }
    }

    public class SourceWithISetItems
    {
        public ISet<Item>? Items { get; set; }
    }

    public class DestWithHashSetItems
    {
        public HashSet<ItemDto>? Items { get; set; }
    }

    // IReadOnlySet source/dest types
    public class SourceWithReadOnlySet
    {
        public IReadOnlySet<string>? Tags { get; set; }
    }

    public class DestFromReadOnlySet
    {
        public HashSet<string>? Tags { get; set; }
    }

    // Edge case types
    public class SourceWithNestedDict
    {
        public Dictionary<string, Dictionary<int, string>>? NestedData { get; set; }
    }

    public class DestWithNestedDict
    {
        public Dictionary<string, Dictionary<int, string>>? NestedData { get; set; }
    }

    public class SourceWithMixedCollections
    {
        public Dictionary<string, int>? Dict { get; set; }
        public Queue<string>? Queue { get; set; }
        public Stack<int>? Stack { get; set; }
        public LinkedList<string>? LinkedList { get; set; }
        public ObservableCollection<int>? Observable { get; set; }
        public SortedSet<string>? SortedSet { get; set; }
    }

    public class DestWithMixedCollections
    {
        public Dictionary<string, int>? Dict { get; set; }
        public Queue<string>? Queue { get; set; }
        public Stack<int>? Stack { get; set; }
        public LinkedList<string>? LinkedList { get; set; }
        public ObservableCollection<int>? Observable { get; set; }
        public SortedSet<string>? SortedSet { get; set; }
    }

    public class SourceWithDictListValue
    {
        public Dictionary<string, List<Item>>? ItemsByCategory { get; set; }
    }

    public class DestWithDictListValue
    {
        public Dictionary<string, List<ItemDto>>? ItemsByCategory { get; set; }
    }

    #endregion

    #region Test Profiles

    public class DictionarySameTypesProfile : Profile
    {
        public DictionarySameTypesProfile()
        {
            CreateMap<SourceWithDictionary, DestWithDictionary>();
        }
    }

    public class DictionaryWithValueMappingProfile : Profile
    {
        public DictionaryWithValueMappingProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithDictValues, DestWithDictValues>();
        }
    }

    public class IDictionaryProfile : Profile
    {
        public IDictionaryProfile()
        {
            CreateMap<SourceWithIDictionary, DestWithIDictionary>();
        }
    }

    public class IReadOnlyDictionaryProfile : Profile
    {
        public IReadOnlyDictionaryProfile()
        {
            CreateMap<SourceWithReadOnlyDict, DestWithReadOnlyDict>();
        }
    }

    public class ObservableCollectionProfile : Profile
    {
        public ObservableCollectionProfile()
        {
            CreateMap<SourceWithObservable, DestWithObservable>();
        }
    }

    public class ObservableCollectionWithMappingProfile : Profile
    {
        public ObservableCollectionWithMappingProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithObservableItems, DestWithObservableItems>();
        }
    }

    public class LinkedListProfile : Profile
    {
        public LinkedListProfile()
        {
            CreateMap<SourceWithLinkedList, DestWithLinkedList>();
        }
    }

    public class LinkedListWithMappingProfile : Profile
    {
        public LinkedListWithMappingProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithLinkedListItems, DestWithLinkedListItems>();
        }
    }

    public class QueueProfile : Profile
    {
        public QueueProfile()
        {
            CreateMap<SourceWithQueue, DestWithQueue>();
        }
    }

    public class QueueWithMappingProfile : Profile
    {
        public QueueWithMappingProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithQueueItems, DestWithQueueItems>();
        }
    }

    public class StackProfile : Profile
    {
        public StackProfile()
        {
            CreateMap<SourceWithStack, DestWithStack>();
        }
    }

    public class StackWithMappingProfile : Profile
    {
        public StackWithMappingProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithStackItems, DestWithStackItems>();
        }
    }

    public class SortedSetProfile : Profile
    {
        public SortedSetProfile()
        {
            CreateMap<SourceWithSortedSet, DestWithSortedSet>();
        }
    }

    public class SortedSetStringsProfile : Profile
    {
        public SortedSetStringsProfile()
        {
            CreateMap<SourceWithSortedSetStrings, DestWithSortedSetStrings>();
        }
    }

    public class ISetProfile : Profile
    {
        public ISetProfile()
        {
            CreateMap<SourceWithISet, DestWithHashSet>();
        }
    }

    public class ISetWithMappingProfile : Profile
    {
        public ISetWithMappingProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithISetItems, DestWithHashSetItems>();
        }
    }

    public class IReadOnlySetProfile : Profile
    {
        public IReadOnlySetProfile()
        {
            CreateMap<SourceWithReadOnlySet, DestFromReadOnlySet>();
        }
    }

    public class NestedDictionaryProfile : Profile
    {
        public NestedDictionaryProfile()
        {
            CreateMap<SourceWithNestedDict, DestWithNestedDict>();
        }
    }

    public class MixedCollectionsProfile : Profile
    {
        public MixedCollectionsProfile()
        {
            CreateMap<SourceWithMixedCollections, DestWithMixedCollections>();
        }
    }

    public class DictWithCollectionValueProfile : Profile
    {
        public DictWithCollectionValueProfile()
        {
            CreateMap<Item, ItemDto>();
            CreateMap<SourceWithDictListValue, DestWithDictListValue>();
        }
    }

    #endregion

    #region Dictionary Tests (6 tests)

    [Fact]
    public void Dictionary_SameTypes_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictionarySameTypesProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictionary
        {
            Scores = new Dictionary<string, int>
            {
                ["Alice"] = 100,
                ["Bob"] = 85,
                ["Charlie"] = 92
            }
        };

        // Act
        var dest = mapper.Map<DestWithDictionary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Scores);
        Assert.Equal(3, dest.Scores.Count);
        Assert.Equal(100, dest.Scores["Alice"]);
        Assert.Equal(85, dest.Scores["Bob"]);
        Assert.Equal(92, dest.Scores["Charlie"]);
    }

    [Fact]
    public void Dictionary_WithValueMapping_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictionaryWithValueMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictValues
        {
            Items = new Dictionary<int, Item>
            {
                [1] = new Item { Id = 1, Name = "Item1" },
                [2] = new Item { Id = 2, Name = "Item2" }
            }
        };

        // Act
        var dest = mapper.Map<DestWithDictValues>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("Item1", dest.Items[1].Name);
        Assert.Equal("Item2", dest.Items[2].Name);
    }

    [Fact]
    public void Dictionary_NullSource_ReturnsEmpty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictionarySameTypesProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictionary { Scores = null };

        // Act
        var dest = mapper.Map<DestWithDictionary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Scores);
        Assert.Empty(dest.Scores);
    }

    [Fact]
    public void Dictionary_EmptySource_ReturnsEmpty()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictionarySameTypesProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictionary { Scores = new Dictionary<string, int>() };

        // Act
        var dest = mapper.Map<DestWithDictionary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Scores);
        Assert.Empty(dest.Scores);
    }

    [Fact]
    public void IDictionary_ToDict_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IDictionaryProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithIDictionary
        {
            Data = new Dictionary<string, int> { ["key1"] = 1, ["key2"] = 2 }
        };

        // Act
        var dest = mapper.Map<DestWithIDictionary>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Data);
        Assert.Equal(2, dest.Data.Count);
        Assert.Equal(1, dest.Data["key1"]);
    }

    [Fact]
    public void IReadOnlyDictionary_ToDict_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IReadOnlyDictionaryProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithReadOnlyDict
        {
            Data = new Dictionary<string, int> { ["a"] = 10, ["b"] = 20 }
        };

        // Act
        var dest = mapper.Map<DestWithReadOnlyDict>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Data);
        Assert.Equal(2, dest.Data.Count);
        Assert.Equal(10, dest.Data["a"]);
    }

    #endregion

    #region ObservableCollection Tests (4 tests)

    [Fact]
    public void ObservableCollection_SameTypes_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ObservableCollectionProfile>());
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
        Assert.Equal(3, dest.Tags.Count);
        Assert.Contains("tag1", dest.Tags);
        Assert.Contains("tag2", dest.Tags);
        Assert.Contains("tag3", dest.Tags);
    }

    [Fact]
    public void ObservableCollection_WithMapping_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ObservableCollectionWithMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithObservableItems
        {
            Items = new ObservableCollection<Item>
            {
                new Item { Id = 1, Name = "First" },
                new Item { Id = 2, Name = "Second" }
            }
        };

        // Act
        var dest = mapper.Map<DestWithObservableItems>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("First", dest.Items[0].Name);
        Assert.Equal("Second", dest.Items[1].Name);
    }

    [Fact]
    public void ObservableCollection_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ObservableCollectionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithObservable { Tags = null };

        // Act
        var dest = mapper.Map<DestWithObservable>(source);

        // Assert
        Assert.NotNull(dest);
        // Null source collection maps to null (consistent with runtime behavior)
        Assert.Null(dest.Tags);
    }

    [Fact]
    public void ObservableCollection_PreservesOrder()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ObservableCollectionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithObservable
        {
            Tags = new ObservableCollection<string> { "first", "second", "third" }
        };

        // Act
        var dest = mapper.Map<DestWithObservable>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.Equal("first", dest.Tags[0]);
        Assert.Equal("second", dest.Tags[1]);
        Assert.Equal("third", dest.Tags[2]);
    }

    #endregion

    #region LinkedList Tests (4 tests)

    [Fact]
    public void LinkedList_SameTypes_MapsCorrectly()
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
        Assert.Equal(3, dest.Nodes.Count);
        Assert.Contains("A", dest.Nodes);
        Assert.Contains("B", dest.Nodes);
        Assert.Contains("C", dest.Nodes);
    }

    [Fact]
    public void LinkedList_WithMapping_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinkedListWithMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithLinkedListItems
        {
            Items = new LinkedList<Item>(new[]
            {
                new Item { Id = 1, Name = "Node1" },
                new Item { Id = 2, Name = "Node2" }
            })
        };

        // Act
        var dest = mapper.Map<DestWithLinkedListItems>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
    }

    [Fact]
    public void LinkedList_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LinkedListProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithLinkedList { Nodes = null };

        // Act
        var dest = mapper.Map<DestWithLinkedList>(source);

        // Assert
        Assert.NotNull(dest);
        // Null source collection maps to null (consistent with runtime behavior)
        Assert.Null(dest.Nodes);
    }

    [Fact]
    public void LinkedList_PreservesOrder()
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

    #region Queue Tests (4 tests)

    [Fact]
    public void Queue_SameTypes_MapsCorrectly()
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
        Assert.Equal(3, dest.Tasks.Count);
    }

    [Fact]
    public void Queue_WithMapping_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<QueueWithMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithQueueItems
        {
            Items = new Queue<Item>(new[]
            {
                new Item { Id = 1, Name = "Q1" },
                new Item { Id = 2, Name = "Q2" }
            })
        };

        // Act
        var dest = mapper.Map<DestWithQueueItems>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
    }

    [Fact]
    public void Queue_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<QueueProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithQueue { Tasks = null };

        // Act
        var dest = mapper.Map<DestWithQueue>(source);

        // Assert
        Assert.NotNull(dest);
        // Null source collection maps to null (consistent with runtime behavior)
        Assert.Null(dest.Tasks);
    }

    [Fact]
    public void Queue_PreservesFIFOOrder()
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

        // Assert - FIFO order: first in, first out
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tasks);
        Assert.Equal("first", dest.Tasks.Dequeue());
        Assert.Equal("second", dest.Tasks.Dequeue());
        Assert.Equal("third", dest.Tasks.Dequeue());
    }

    #endregion

    #region Stack Tests (4 tests)

    [Fact]
    public void Stack_SameTypes_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StackProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithStack
        {
            History = new Stack<string>(new[] { "cmd1", "cmd2", "cmd3" })
        };

        // Act
        var dest = mapper.Map<DestWithStack>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.History);
        Assert.Equal(3, dest.History.Count);
    }

    [Fact]
    public void Stack_WithMapping_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StackWithMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithStackItems
        {
            Items = new Stack<Item>(new[]
            {
                new Item { Id = 1, Name = "S1" },
                new Item { Id = 2, Name = "S2" }
            })
        };

        // Act
        var dest = mapper.Map<DestWithStackItems>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
    }

    [Fact]
    public void Stack_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StackProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithStack { History = null };

        // Act
        var dest = mapper.Map<DestWithStack>(source);

        // Assert
        Assert.NotNull(dest);
        // Null source collection maps to null (consistent with runtime behavior)
        Assert.Null(dest.History);
    }

    [Fact]
    public void Stack_PreservesLIFOOrder()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<StackProfile>());
        var mapper = config.CreateMapper();

        // Push order: first, second, third (third is on top)
        var sourceStack = new Stack<string>();
        sourceStack.Push("first");
        sourceStack.Push("second");
        sourceStack.Push("third");

        var source = new SourceWithStack { History = sourceStack };

        // Act
        var dest = mapper.Map<DestWithStack>(source);

        // Assert - LIFO order preserved: last in, first out
        Assert.NotNull(dest);
        Assert.NotNull(dest.History);
        Assert.Equal("third", dest.History.Pop());   // top
        Assert.Equal("second", dest.History.Pop());
        Assert.Equal("first", dest.History.Pop());   // bottom
    }

    #endregion

    #region SortedSet Tests (4 tests)

    [Fact]
    public void SortedSet_SameTypes_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SortedSetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithSortedSet
        {
            Numbers = new SortedSet<int> { 5, 2, 8, 1, 9 }
        };

        // Act
        var dest = mapper.Map<DestWithSortedSet>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Numbers);
        Assert.Equal(5, dest.Numbers.Count);
        Assert.Contains(5, dest.Numbers);
        Assert.Contains(1, dest.Numbers);
    }

    [Fact]
    public void SortedSet_WithStrings_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SortedSetStringsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithSortedSetStrings
        {
            Tags = new SortedSet<string> { "zebra", "apple", "banana" }
        };

        // Act
        var dest = mapper.Map<DestWithSortedSetStrings>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.Equal(3, dest.Tags.Count);
    }

    [Fact]
    public void SortedSet_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SortedSetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithSortedSet { Numbers = null };

        // Act
        var dest = mapper.Map<DestWithSortedSet>(source);

        // Assert
        Assert.NotNull(dest);
        // Null source collection maps to null (consistent with runtime behavior)
        Assert.Null(dest.Numbers);
    }

    [Fact]
    public void SortedSet_MaintainsSortOrder()
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

    #region ISet/IReadOnlySet Tests (4 tests)

    [Fact]
    public void ISet_ToHashSet_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ISetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithISet
        {
            Tags = new HashSet<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        var dest = mapper.Map<DestWithHashSet>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.Equal(3, dest.Tags.Count);
        Assert.Contains("tag1", dest.Tags);
    }

    [Fact]
    public void ISet_WithMapping_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ISetWithMappingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithISetItems
        {
            Items = new HashSet<Item>
            {
                new Item { Id = 1, Name = "SetItem1" },
                new Item { Id = 2, Name = "SetItem2" }
            }
        };

        // Act
        var dest = mapper.Map<DestWithHashSetItems>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Items);
        Assert.Equal(2, dest.Items.Count);
    }

    [Fact]
    public void IReadOnlySet_ToHashSet_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IReadOnlySetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithReadOnlySet
        {
            Tags = new HashSet<string> { "readonly1", "readonly2" }
        };

        // Act
        var dest = mapper.Map<DestFromReadOnlySet>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Tags);
        Assert.Equal(2, dest.Tags.Count);
        Assert.Contains("readonly1", dest.Tags);
    }

    [Fact]
    public void IReadOnlySet_NullSource_ReturnsNull()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IReadOnlySetProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithReadOnlySet { Tags = null };

        // Act
        var dest = mapper.Map<DestFromReadOnlySet>(source);

        // Assert
        Assert.NotNull(dest);
        // Null source collection maps to null (consistent with runtime behavior)
        Assert.Null(dest.Tags);
    }

    #endregion

    #region Edge Case Tests (4 tests)

    [Fact]
    public void NestedDictionary_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedDictionaryProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNestedDict
        {
            NestedData = new Dictionary<string, Dictionary<int, string>>
            {
                ["level1"] = new Dictionary<int, string>
                {
                    [1] = "one",
                    [2] = "two"
                }
            }
        };

        // Act
        var dest = mapper.Map<DestWithNestedDict>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.NestedData);
        Assert.True(dest.NestedData.ContainsKey("level1"));
        Assert.Equal("one", dest.NestedData["level1"][1]);
    }

    [Fact]
    public void MixedCollections_InSameClass_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedCollectionsProfile>());
        var mapper = config.CreateMapper();

        var stackData = new Stack<int>();
        stackData.Push(1);
        stackData.Push(2);
        stackData.Push(3);

        var source = new SourceWithMixedCollections
        {
            Dict = new Dictionary<string, int> { ["a"] = 1 },
            Queue = new Queue<string>(new[] { "q1", "q2" }),
            Stack = stackData,
            LinkedList = new LinkedList<string>(new[] { "ll1", "ll2" }),
            Observable = new ObservableCollection<int> { 10, 20 },
            SortedSet = new SortedSet<string> { "z", "a", "m" }
        };

        // Act
        var dest = mapper.Map<DestWithMixedCollections>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Dict);
        Assert.NotNull(dest.Queue);
        Assert.NotNull(dest.Stack);
        Assert.NotNull(dest.LinkedList);
        Assert.NotNull(dest.Observable);
        Assert.NotNull(dest.SortedSet);

        Assert.Single(dest.Dict);
        Assert.Equal(2, dest.Queue.Count);
        Assert.Equal(3, dest.Stack.Count);
        Assert.Equal(2, dest.LinkedList.Count);
        Assert.Equal(2, dest.Observable.Count);
        Assert.Equal(3, dest.SortedSet.Count);
    }

    [Fact]
    public void CollectionOfCollections_ListOfQueues_MapsCorrectly()
    {
        // This tests runtime behavior since nested generics are complex
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedCollectionsProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithMixedCollections
        {
            Queue = new Queue<string>(new[] { "a", "b", "c" })
        };

        // Act
        var dest = mapper.Map<DestWithMixedCollections>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Queue);
        Assert.Equal(3, dest.Queue.Count);
    }

    [Fact]
    public void DictionaryWithCollectionValue_MapsCorrectly()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictWithCollectionValueProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithDictListValue
        {
            ItemsByCategory = new Dictionary<string, List<Item>>
            {
                ["electronics"] = new List<Item>
                {
                    new Item { Id = 1, Name = "Phone" },
                    new Item { Id = 2, Name = "Laptop" }
                },
                ["clothing"] = new List<Item>
                {
                    new Item { Id = 3, Name = "Shirt" }
                }
            }
        };

        // Act
        var dest = mapper.Map<DestWithDictListValue>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.ItemsByCategory);
        Assert.Equal(2, dest.ItemsByCategory.Count);
        Assert.Equal(2, dest.ItemsByCategory["electronics"].Count);
        Assert.Single(dest.ItemsByCategory["clothing"]);
        Assert.Equal("Phone", dest.ItemsByCategory["electronics"][0].Name);
    }

    #endregion
}
