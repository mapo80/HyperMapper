using System.Collections.ObjectModel;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 CollectionMapping.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class AdvancedCollectionPortedTests
{
    #region IReadOnlySet Mapping Tests

    [Fact]
    public void Should_map_to_IReadOnlySet()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlySetProfile>());
        var mapper = config.CreateMapper();

        var source = new HashSetSource
        {
            Values = new HashSet<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<ReadOnlySetDest>(source);

        Assert.Equal(3, dest.Values.Count);
        Assert.Contains(1, dest.Values);
        Assert.Contains(2, dest.Values);
        Assert.Contains(3, dest.Values);
    }

    [Fact]
    public void Should_map_HashSet_to_IReadOnlySet()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ReadOnlySetProfile>());
        var mapper = config.CreateMapper();

        var source = new HashSetSource
        {
            Values = new HashSet<int> { 1, 2, 3, 2, 1 } // Duplicates should be removed
        };

        var dest = mapper.Map<ReadOnlySetDest>(source);

        Assert.Equal(3, dest.Values.Count);
    }

    #endregion

    #region HashSet Mapping Tests

    [Fact]
    public void Should_map_List_to_HashSet()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<HashSetProfile>());
        var mapper = config.CreateMapper();

        var source = new ListCollSource
        {
            Values = new List<int> { 1, 2, 3, 2, 1 }
        };

        var dest = mapper.Map<HashSetDest>(source);

        Assert.Equal(3, dest.Values.Count); // Duplicates removed
    }

    [Fact]
    public void Should_map_HashSet_to_List()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<HashSetProfile>());
        var mapper = config.CreateMapper();

        var source = new HashSetCollSource
        {
            Values = new HashSet<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<ListCollDest>(source);

        Assert.Equal(3, dest.Values.Count);
    }

    #endregion

    #region ObservableCollection Mapping Tests

    [Fact]
    public void Should_map_to_ObservableCollection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ObservableCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new ListObsSource
        {
            Items = new List<ObsItemSource>
            {
                new() { Name = "A" },
                new() { Name = "B" }
            }
        };

        var dest = mapper.Map<ObservableCollectionDest>(source);

        Assert.IsType<ObservableCollection<ObsItemDest>>(dest.Items);
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("A", dest.Items[0].Name);
    }

    [Fact]
    public void Should_map_from_ObservableCollection()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ObservableCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new ObservableCollectionSource
        {
            Items = new ObservableCollection<ObsItemSource>
            {
                new() { Name = "A" },
                new() { Name = "B" }
            }
        };

        var dest = mapper.Map<ListObsDest>(source);

        Assert.IsType<List<ObsItemDest>>(dest.Items);
        Assert.Equal(2, dest.Items.Count);
    }

    #endregion

    #region IEnumerable Member Typing Tests

    [Fact]
    public void Should_map_IEnumerable_member_to_List()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<EnumerableMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new EnumerableSource
        {
            Items = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<ListMemberDest>(source);

        Assert.Equal(3, dest.Items.Count);
    }

    [Fact]
    public void Should_map_List_member_to_IEnumerable()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<EnumerableMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new ListMemberSource
        {
            Items = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<EnumerableDest>(source);

        Assert.Equal(3, dest.Items.Count());
    }

    #endregion

    #region Struct Collection Mapping Tests

    [Fact]
    public void Should_map_collection_of_structs()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<StructCollProfile>());
        var mapper = config.CreateMapper();

        var source = new StructCollSource
        {
            Points = new List<PointStruct>
            {
                new() { X = 1, Y = 2 },
                new() { X = 3, Y = 4 }
            }
        };

        var dest = mapper.Map<StructCollDest>(source);

        Assert.Equal(2, dest.Points.Count);
        Assert.Equal(1, dest.Points[0].X);
    }

    #endregion

    #region Custom Collection Type Mapping Tests

    [Fact]
    public void Should_map_to_custom_collection_type()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CustomCollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new StandardCollSource
        {
            Items = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<CustomCollDest>(source);

        Assert.IsType<AdvancedCustomCollection<int>>(dest.Items);
        Assert.Equal(3, dest.Items.Count);
    }

    #endregion

    #region Collection Preservation Tests

    [Fact]
    public void Should_map_and_fill_destination_list()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CollPreserveProfile>());
        var mapper = config.CreateMapper();

        var source = new CollPreserveSource
        {
            Items = new List<CollItemSource>
            {
                new() { Id = 1, Value = "A" },
                new() { Id = 2, Value = "B" }
            }
        };

        var dest = mapper.Map<CollPreserveDest>(source);

        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("A", dest.Items[0].Value);
        Assert.Equal("B", dest.Items[1].Value);
    }

    #endregion

    #region ICollection<T> Implementation Tests

    [Fact]
    public void Should_map_to_ICollection_implementation()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ICollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new ICollectionSource
        {
            Items = new List<string> { "a", "b", "c" }
        };

        var dest = mapper.Map<ICollectionDest>(source);

        Assert.Equal(3, dest.Items.Count);
    }

    #endregion

    #region LinkedList Mapping Tests

    [Fact]
    public void Should_map_to_LinkedList()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<LinkedListProfile>());
        var mapper = config.CreateMapper();

        var source = new ListLinkedSource
        {
            Values = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<LinkedListDest>(source);

        Assert.IsType<LinkedList<int>>(dest.Values);
        Assert.Equal(3, dest.Values.Count);
    }

    #endregion

    #region Queue and Stack Mapping Tests

    [Fact]
    public void Should_map_to_Queue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<QueueStackProfile>());
        var mapper = config.CreateMapper();

        var source = new ListQueueSource
        {
            Values = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<QueueDest>(source);

        Assert.IsType<Queue<int>>(dest.Values);
        Assert.Equal(3, dest.Values.Count);
    }

    [Fact]
    public void Should_map_to_Stack()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<QueueStackProfile>());
        var mapper = config.CreateMapper();

        var source = new ListStackSource
        {
            Values = new List<int> { 1, 2, 3 }
        };

        var dest = mapper.Map<StackDest>(source);

        Assert.IsType<Stack<int>>(dest.Values);
        Assert.Equal(3, dest.Values.Count);
    }

    #endregion
}

#region Test Classes and Profiles

// IReadOnlySet
public class HashSetSource { public HashSet<int> Values { get; set; } = new(); }
public class ReadOnlySetDest { public IReadOnlySet<int> Values { get; set; } = new HashSet<int>(); }

public class ReadOnlySetProfile : Profile
{
    public ReadOnlySetProfile()
    {
        CreateMap<HashSetSource, ReadOnlySetDest>();
    }
}

// HashSet
public class ListCollSource { public List<int> Values { get; set; } = new(); }
public class HashSetDest { public HashSet<int> Values { get; set; } = new(); }
public class HashSetCollSource { public HashSet<int> Values { get; set; } = new(); }
public class ListCollDest { public List<int> Values { get; set; } = new(); }

public class HashSetProfile : Profile
{
    public HashSetProfile()
    {
        CreateMap<ListCollSource, HashSetDest>();
        CreateMap<HashSetCollSource, ListCollDest>();
    }
}

// ObservableCollection
public class ObsItemSource { public string Name { get; set; } = string.Empty; }
public class ObsItemDest { public string Name { get; set; } = string.Empty; }
public class ListObsSource { public List<ObsItemSource> Items { get; set; } = new(); }
public class ObservableCollectionDest { public ObservableCollection<ObsItemDest> Items { get; set; } = new(); }
public class ObservableCollectionSource { public ObservableCollection<ObsItemSource> Items { get; set; } = new(); }
public class ListObsDest { public List<ObsItemDest> Items { get; set; } = new(); }

public class ObservableCollectionProfile : Profile
{
    public ObservableCollectionProfile()
    {
        CreateMap<ObsItemSource, ObsItemDest>();
        CreateMap<ListObsSource, ObservableCollectionDest>();
        CreateMap<ObservableCollectionSource, ListObsDest>();
    }
}

// IEnumerable Member
public class EnumerableSource { public IEnumerable<int> Items { get; set; } = Enumerable.Empty<int>(); }
public class ListMemberDest { public List<int> Items { get; set; } = new(); }
public class ListMemberSource { public List<int> Items { get; set; } = new(); }
public class EnumerableDest { public IEnumerable<int> Items { get; set; } = Enumerable.Empty<int>(); }

public class EnumerableMemberProfile : Profile
{
    public EnumerableMemberProfile()
    {
        CreateMap<EnumerableSource, ListMemberDest>();
        CreateMap<ListMemberSource, EnumerableDest>();
    }
}

// Struct Collection
public struct PointStruct { public int X { get; set; } public int Y { get; set; } }
public struct PointStructDest { public int X { get; set; } public int Y { get; set; } }
public class StructCollSource { public List<PointStruct> Points { get; set; } = new(); }
public class StructCollDest { public List<PointStructDest> Points { get; set; } = new(); }

public class StructCollProfile : Profile
{
    public StructCollProfile()
    {
        CreateMap<PointStruct, PointStructDest>();
        CreateMap<StructCollSource, StructCollDest>();
    }
}

// Custom Collection
public class AdvancedCustomCollection<T> : List<T> { }

public class StandardCollSource { public List<int> Items { get; set; } = new(); }
public class CustomCollDest { public AdvancedCustomCollection<int> Items { get; set; } = new(); }

public class CustomCollectionProfile : Profile
{
    public CustomCollectionProfile()
    {
        CreateMap<StandardCollSource, CustomCollDest>();
    }
}

// Collection Preservation
public class CollItemSource { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public class CollItemDest { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public class CollPreserveSource { public List<CollItemSource> Items { get; set; } = new(); }
public class CollPreserveDest { public List<CollItemDest> Items { get; set; } = new(); }

public class CollPreserveProfile : Profile
{
    public CollPreserveProfile()
    {
        CreateMap<CollItemSource, CollItemDest>();
        CreateMap<CollPreserveSource, CollPreserveDest>();
    }
}

// ICollection
public class ICollectionSource { public ICollection<string> Items { get; set; } = new List<string>(); }
public class ICollectionDest { public ICollection<string> Items { get; set; } = new List<string>(); }

public class ICollectionProfile : Profile
{
    public ICollectionProfile()
    {
        CreateMap<ICollectionSource, ICollectionDest>();
    }
}

// LinkedList
public class ListLinkedSource { public List<int> Values { get; set; } = new(); }
public class LinkedListDest { public LinkedList<int> Values { get; set; } = new(); }

public class LinkedListProfile : Profile
{
    public LinkedListProfile()
    {
        CreateMap<ListLinkedSource, LinkedListDest>();
    }
}

// Queue and Stack
public class ListQueueSource { public List<int> Values { get; set; } = new(); }
public class QueueDest { public Queue<int> Values { get; set; } = new(); }
public class ListStackSource { public List<int> Values { get; set; } = new(); }
public class StackDest { public Stack<int> Values { get; set; } = new(); }

public class QueueStackProfile : Profile
{
    public QueueStackProfile()
    {
        CreateMap<ListQueueSource, QueueDest>();
        CreateMap<ListStackSource, StackDest>();
    }
}

#endregion
