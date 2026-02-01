using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace HyperMapper.Internal;

/// <summary>
/// Factory for creating and populating collections with compiled delegates.
/// Avoids MethodInfo.Invoke overhead in hot paths.
/// </summary>
internal static class CollectionFactory
{
    private static readonly ConcurrentDictionary<Type, ICollectionBuilder> _builders = new();

    /// <summary>
    /// Gets or creates a collection builder for the specified collection type.
    /// </summary>
    public static ICollectionBuilder GetBuilder(Type collectionType, Type elementType)
    {
        return _builders.GetOrAdd(collectionType, t => CreateBuilder(t, elementType));
    }

    private static ICollectionBuilder CreateBuilder(Type collectionType, Type elementType)
    {
        if (collectionType.IsArray)
        {
            return new ArrayBuilder(elementType);
        }

        if (collectionType.IsGenericType)
        {
            var genericDef = collectionType.GetGenericTypeDefinition();

            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                return CreateListBuilder(listType, elementType);
            }

            if (genericDef == typeof(HashSet<>))
            {
                return CreateHashSetBuilder(collectionType, elementType);
            }

            if (genericDef == typeof(ObservableCollection<>))
            {
                return CreateObservableCollectionBuilder(collectionType, elementType);
            }

            if (genericDef == typeof(LinkedList<>))
            {
                return CreateLinkedListBuilder(collectionType, elementType);
            }

            if (genericDef == typeof(Queue<>))
            {
                return CreateQueueBuilder(collectionType, elementType);
            }

            if (genericDef == typeof(Stack<>))
            {
                return CreateStackBuilder(collectionType, elementType);
            }
        }

        // Check for ICollection<T> interface (custom collections)
        var collectionInterface = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

        if (collectionInterface != null && !collectionType.IsInterface && !collectionType.IsAbstract)
        {
            return CreateGenericCollectionBuilder(collectionType, elementType);
        }

        // Default: use List<T>
        var defaultListType = typeof(List<>).MakeGenericType(elementType);
        return CreateListBuilder(defaultListType, elementType);
    }

    private static ICollectionBuilder CreateListBuilder(Type listType, Type elementType)
    {
        var builderType = typeof(ListBuilder<>).MakeGenericType(elementType);
        return (ICollectionBuilder)Activator.CreateInstance(builderType)!;
    }

    private static ICollectionBuilder CreateHashSetBuilder(Type setType, Type elementType)
    {
        var builderType = typeof(HashSetBuilder<>).MakeGenericType(elementType);
        return (ICollectionBuilder)Activator.CreateInstance(builderType)!;
    }

    private static ICollectionBuilder CreateObservableCollectionBuilder(Type collectionType, Type elementType)
    {
        var builderType = typeof(ObservableCollectionBuilder<>).MakeGenericType(elementType);
        return (ICollectionBuilder)Activator.CreateInstance(builderType)!;
    }

    private static ICollectionBuilder CreateLinkedListBuilder(Type listType, Type elementType)
    {
        var builderType = typeof(LinkedListBuilder<>).MakeGenericType(elementType);
        return (ICollectionBuilder)Activator.CreateInstance(builderType)!;
    }

    private static ICollectionBuilder CreateQueueBuilder(Type queueType, Type elementType)
    {
        var builderType = typeof(QueueBuilder<>).MakeGenericType(elementType);
        return (ICollectionBuilder)Activator.CreateInstance(builderType)!;
    }

    private static ICollectionBuilder CreateStackBuilder(Type stackType, Type elementType)
    {
        var builderType = typeof(StackBuilder<>).MakeGenericType(elementType);
        return (ICollectionBuilder)Activator.CreateInstance(builderType)!;
    }

    private static ICollectionBuilder CreateGenericCollectionBuilder(Type collectionType, Type elementType)
    {
        // For custom collections, we still need reflection but cache the Add delegate
        var addMethod = collectionType.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
            .GetMethod("Add")!;

        // Create compiled Add delegate
        var collectionParam = Expression.Parameter(typeof(object), "collection");
        var itemParam = Expression.Parameter(typeof(object), "item");
        var castCollection = Expression.Convert(collectionParam, collectionType);
        var castItem = Expression.Convert(itemParam, elementType);
        var addCall = Expression.Call(castCollection, addMethod, castItem);
        var addDelegate = Expression.Lambda<Action<object, object?>>(addCall, collectionParam, itemParam).Compile();

        return new GenericCollectionBuilder(collectionType, addDelegate);
    }

    internal interface ICollectionBuilder
    {
        object Create(int capacity);
        void Add(object collection, object? item);
        object BuildFromItems(List<object?> items);
    }

    private class ArrayBuilder : ICollectionBuilder
    {
        private readonly Type _elementType;

        public ArrayBuilder(Type elementType)
        {
            _elementType = elementType;
        }

        public object Create(int capacity) => Array.CreateInstance(_elementType, capacity);

        public void Add(object collection, object? item)
        {
            throw new NotSupportedException("Arrays don't support Add. Use BuildFromItems instead.");
        }

        public object BuildFromItems(List<object?> items)
        {
            var array = Array.CreateInstance(_elementType, items.Count);
            for (int i = 0; i < items.Count; i++)
                array.SetValue(items[i], i);
            return array;
        }
    }

    private class ListBuilder<T> : ICollectionBuilder
    {
        public object Create(int capacity) => new List<T>(capacity);

        public void Add(object collection, object? item) => ((List<T>)collection).Add((T)item!);

        public object BuildFromItems(List<object?> items)
        {
            var list = new List<T>(items.Count);
            foreach (var item in items)
                list.Add((T)item!);
            return list;
        }
    }

    private class HashSetBuilder<T> : ICollectionBuilder
    {
        public object Create(int capacity) => new HashSet<T>(capacity);

        public void Add(object collection, object? item) => ((HashSet<T>)collection).Add((T)item!);

        public object BuildFromItems(List<object?> items)
        {
            var set = new HashSet<T>(items.Count);
            foreach (var item in items)
                set.Add((T)item!);
            return set;
        }
    }

    private class ObservableCollectionBuilder<T> : ICollectionBuilder
    {
        public object Create(int capacity) => new ObservableCollection<T>();

        public void Add(object collection, object? item) => ((ObservableCollection<T>)collection).Add((T)item!);

        public object BuildFromItems(List<object?> items)
        {
            var collection = new ObservableCollection<T>();
            foreach (var item in items)
                collection.Add((T)item!);
            return collection;
        }
    }

    private class LinkedListBuilder<T> : ICollectionBuilder
    {
        public object Create(int capacity) => new LinkedList<T>();

        public void Add(object collection, object? item) => ((LinkedList<T>)collection).AddLast((T)item!);

        public object BuildFromItems(List<object?> items)
        {
            var list = new LinkedList<T>();
            foreach (var item in items)
                list.AddLast((T)item!);
            return list;
        }
    }

    private class QueueBuilder<T> : ICollectionBuilder
    {
        public object Create(int capacity) => new Queue<T>(capacity);

        public void Add(object collection, object? item) => ((Queue<T>)collection).Enqueue((T)item!);

        public object BuildFromItems(List<object?> items)
        {
            var queue = new Queue<T>(items.Count);
            foreach (var item in items)
                queue.Enqueue((T)item!);
            return queue;
        }
    }

    private class StackBuilder<T> : ICollectionBuilder
    {
        public object Create(int capacity) => new Stack<T>(capacity);

        public void Add(object collection, object? item) => ((Stack<T>)collection).Push((T)item!);

        public object BuildFromItems(List<object?> items)
        {
            var stack = new Stack<T>(items.Count);
            foreach (var item in items)
                stack.Push((T)item!);
            return stack;
        }
    }

    private class GenericCollectionBuilder : ICollectionBuilder
    {
        private readonly Type _collectionType;
        private readonly Action<object, object?> _addDelegate;

        public GenericCollectionBuilder(Type collectionType, Action<object, object?> addDelegate)
        {
            _collectionType = collectionType;
            _addDelegate = addDelegate;
        }

        public object Create(int capacity) => ReflectionCache.CreateInstance(_collectionType);

        public void Add(object collection, object? item) => _addDelegate(collection, item);

        public object BuildFromItems(List<object?> items)
        {
            var collection = Create(items.Count);
            foreach (var item in items)
                _addDelegate(collection, item);
            return collection;
        }
    }
}
