using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace HyperMapper.Internal;

/// <summary>
/// Caches reflection metadata and compiled accessors for performance.
/// </summary>
internal static class ReflectionCache
{
    // PropertyInfo cache per type
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _writableProperties = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _writablePropertiesDict = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _readableProperties = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _readablePropertiesDict = new();

    // Compiled getter/setter cache
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> _getters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> _setters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?, object>> _valueTypeSetters = new();

    // Constructor/factory cache
    private static readonly ConcurrentDictionary<Type, Func<object>> _factories = new();

    // Interfaces cache
    private static readonly ConcurrentDictionary<Type, Type[]> _interfaces = new();

    // Type analysis cache - caches all type checks in one lookup
    private static readonly ConcurrentDictionary<Type, TypeAnalysisResult> _typeAnalysis = new();

    // Case-insensitive property dictionary cache
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _readablePropertiesDictCI = new();

    /// <summary>
    /// Gets writable properties for a type (cached).
    /// </summary>
    public static PropertyInfo[] GetWritableProperties(Type type)
    {
        return _writableProperties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanWrite)
             .ToArray());
    }

    /// <summary>
    /// Gets writable properties as dictionary with case-sensitive keys (cached).
    /// </summary>
    public static Dictionary<string, PropertyInfo> GetWritablePropertiesDict(Type type)
    {
        return _writablePropertiesDict.GetOrAdd(type, t =>
            GetWritableProperties(t).ToDictionary(p => p.Name, StringComparer.Ordinal));
    }

    /// <summary>
    /// Gets readable properties for a type (cached).
    /// </summary>
    public static PropertyInfo[] GetReadableProperties(Type type)
    {
        return _readableProperties.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead)
             .ToArray());
    }

    /// <summary>
    /// Gets readable properties as dictionary with case-sensitive keys (cached).
    /// </summary>
    public static Dictionary<string, PropertyInfo> GetReadablePropertiesDict(Type type)
    {
        return _readablePropertiesDict.GetOrAdd(type, t =>
            GetReadableProperties(t).ToDictionary(p => p.Name, StringComparer.Ordinal));
    }

    /// <summary>
    /// Gets readable properties as dictionary with case-insensitive keys (cached).
    /// If there are duplicate names (case-insensitive), the first one wins.
    /// </summary>
    public static Dictionary<string, PropertyInfo> GetReadablePropertiesDictCaseInsensitive(Type type)
    {
        return _readablePropertiesDictCI.GetOrAdd(type, t =>
        {
            var dict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in GetReadableProperties(t))
            {
                // TryAdd returns false if key already exists (case-insensitive collision)
                dict.TryAdd(prop.Name, prop);
            }
            return dict;
        });
    }

    /// <summary>
    /// Gets a compiled getter for the property (cached).
    /// </summary>
    public static Func<object, object?> GetGetter(PropertyInfo prop)
    {
        return _getters.GetOrAdd(prop, CreateGetter);
    }

    /// <summary>
    /// Gets a compiled setter for the property (cached).
    /// </summary>
    public static Action<object, object?> GetSetter(PropertyInfo prop)
    {
        return _setters.GetOrAdd(prop, CreateSetter);
    }

    /// <summary>
    /// Gets a compiled factory for creating instances of the type (cached).
    /// </summary>
    public static Func<object> GetFactory(Type type)
    {
        return _factories.GetOrAdd(type, CreateFactory);
    }

    /// <summary>
    /// Gets interfaces for a type (cached).
    /// </summary>
    public static Type[] GetInterfaces(Type type)
    {
        return _interfaces.GetOrAdd(type, t => t.GetInterfaces());
    }

    /// <summary>
    /// Gets property value using compiled getter.
    /// </summary>
    public static object? GetValue(PropertyInfo prop, object source)
    {
        return GetGetter(prop)(source);
    }

    /// <summary>
    /// Sets property value using compiled setter.
    /// </summary>
    public static void SetValue(PropertyInfo prop, object target, object? value)
    {
        GetSetter(prop)(target, value);
    }

    /// <summary>
    /// Sets property value on a value type and returns the modified instance.
    /// Needed because value types are copied when boxed.
    /// </summary>
    public static object SetValueOnValueType(PropertyInfo prop, object target, object? value)
    {
        return GetValueTypeSetter(prop)(target, value);
    }

    /// <summary>
    /// Gets a compiled setter for value types that returns the modified instance.
    /// </summary>
    public static Func<object, object?, object> GetValueTypeSetter(PropertyInfo prop)
    {
        return _valueTypeSetters.GetOrAdd(prop, CreateValueTypeSetter);
    }

    private static Func<object, object?, object> CreateValueTypeSetter(PropertyInfo prop)
    {
        var declaringType = prop.DeclaringType!;
        var targetParam = Expression.Parameter(typeof(object), "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // Unbox to value type variable
        var targetVar = Expression.Variable(declaringType, "targetVar");
        var assignTarget = Expression.Assign(targetVar, Expression.Convert(targetParam, declaringType));

        // Set property on the variable
        var valueCast = Expression.Convert(valueParam, prop.PropertyType);
        var setProperty = Expression.Assign(Expression.Property(targetVar, prop), valueCast);

        // Box and return
        var boxed = Expression.Convert(targetVar, typeof(object));

        var block = Expression.Block(
            new[] { targetVar },
            assignTarget,
            setProperty,
            boxed);

        return Expression.Lambda<Func<object, object?, object>>(block, targetParam, valueParam).Compile();
    }

    /// <summary>
    /// Creates instance using compiled factory.
    /// </summary>
    public static object CreateInstance(Type type)
    {
        return GetFactory(type)();
    }

    private static Func<object, object?> CreateGetter(PropertyInfo prop)
    {
        if (prop.GetMethod == null)
            throw new InvalidOperationException($"Property {prop.Name} has no getter.");

        var param = Expression.Parameter(typeof(object), "obj");
        var cast = Expression.Convert(param, prop.DeclaringType!);
        var getCall = Expression.Call(cast, prop.GetMethod);
        var box = Expression.Convert(getCall, typeof(object));

        return Expression.Lambda<Func<object, object?>>(box, param).Compile();
    }

    private static Action<object, object?> CreateSetter(PropertyInfo prop)
    {
        if (prop.SetMethod == null)
            throw new InvalidOperationException($"Property {prop.Name} has no setter.");

        var targetParam = Expression.Parameter(typeof(object), "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var targetCast = Expression.Convert(targetParam, prop.DeclaringType!);
        var valueCast = Expression.Convert(valueParam, prop.PropertyType);

        var setCall = Expression.Call(targetCast, prop.SetMethod, valueCast);

        return Expression.Lambda<Action<object, object?>>(setCall, targetParam, valueParam).Compile();
    }

    private static Func<object> CreateFactory(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return CreateFactory(underlyingType);
        }

        // For value types, use compiled default(T) instead of Activator
        if (type.IsValueType)
        {
            var defaultValue = Expression.Default(type);
            var boxed = Expression.Convert(defaultValue, typeof(object));
            return Expression.Lambda<Func<object>>(boxed).Compile();
        }

        // Try public parameterless constructor first
        var publicCtor = type.GetConstructor(Type.EmptyTypes);
        if (publicCtor != null)
        {
            var newExpr = Expression.New(publicCtor);
            var cast = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object>>(cast).Compile();
        }

        // Try private/protected parameterless constructor
        var privateCtor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (privateCtor != null)
        {
            var newExpr = Expression.New(privateCtor);
            var cast = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object>>(cast).Compile();
        }

        // Fallback to RuntimeHelpers (creates uninitialized object)
        return () => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
    }

    #region Type Analysis Cache

    /// <summary>
    /// Cached result of type analysis to avoid repeated reflection calls.
    /// </summary>
    public sealed record TypeAnalysisResult(
        bool IsEnumerable,
        Type? EnumerableElementType,
        bool IsCollection,
        Type? CollectionElementType,
        bool IsDictionary,
        Type? DictionaryKeyType,
        Type? DictionaryValueType,
        bool IsSimple,
        bool IsNullable,
        Type? UnderlyingNullableType
    );

    /// <summary>
    /// Gets cached type analysis result. All type checks are performed once and cached.
    /// </summary>
    public static TypeAnalysisResult GetTypeAnalysis(Type type)
    {
        return _typeAnalysis.GetOrAdd(type, AnalyzeType);
    }

    private static TypeAnalysisResult AnalyzeType(Type type)
    {
        var underlyingNullable = Nullable.GetUnderlyingType(type);
        var isNullable = !type.IsValueType || underlyingNullable != null;

        var (isEnumerable, enumElementType) = AnalyzeEnumerable(type);
        var (isCollection, collElementType) = AnalyzeCollection(type);
        var (isDictionary, keyType, valueType) = AnalyzeDictionary(type);
        var isSimple = AnalyzeSimple(type);

        return new TypeAnalysisResult(
            isEnumerable, enumElementType,
            isCollection, collElementType,
            isDictionary, keyType, valueType,
            isSimple, isNullable, underlyingNullable
        );
    }

    private static (bool IsEnumerable, Type? ElementType) AnalyzeEnumerable(Type type)
    {
        if (type == typeof(string))
            return (false, null);

        if (type.IsArray)
            return (true, type.GetElementType());

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(System.Collections.ObjectModel.ObservableCollection<>) ||
                genericDef == typeof(LinkedList<>) ||
                genericDef == typeof(Queue<>) ||
                genericDef == typeof(Stack<>))
            {
                return (true, type.GetGenericArguments()[0]);
            }
        }

        // Check for IEnumerable<T> interface
        var enumInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface != null)
            return (true, enumInterface.GetGenericArguments()[0]);

        return (false, null);
    }

    private static (bool IsCollection, Type? ElementType) AnalyzeCollection(Type type)
    {
        if (type == typeof(string))
            return (false, null);

        if (type.IsArray)
            return (true, type.GetElementType());

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(System.Collections.ObjectModel.ObservableCollection<>) ||
                genericDef == typeof(LinkedList<>) ||
                genericDef == typeof(Queue<>) ||
                genericDef == typeof(Stack<>))
            {
                return (true, type.GetGenericArguments()[0]);
            }
        }

        // Check for IEnumerable<T> interface (custom collections)
        var enumInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface != null)
            return (true, enumInterface.GetGenericArguments()[0]);

        return (false, null);
    }

    private static (bool IsDictionary, Type? KeyType, Type? ValueType) AnalyzeDictionary(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>) ||
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                var args = type.GetGenericArguments();
                return (true, args[0], args[1]);
            }
        }

        // Check for IDictionary<,> interface
        var dictInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictInterface != null)
        {
            var args = dictInterface.GetGenericArguments();
            return (true, args[0], args[1]);
        }

        return (false, null, null);
    }

    private static bool AnalyzeSimple(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateOnly) ||
               underlyingType == typeof(TimeOnly) ||
               underlyingType == typeof(TimeSpan) ||
               underlyingType == typeof(Guid) ||
               underlyingType.IsEnum;
    }

    #endregion
}
