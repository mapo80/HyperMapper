using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HyperMapper.Internal;

namespace HyperMapper;

internal class Mapper : IMapper
{
    private readonly TypeMapRegistry _registry;
    // v8.0.0: Store typed delegates directly for maximum performance
    private readonly Dictionary<(Type, Type), Delegate> _generatedPlans;
    private readonly ResolutionContext _context;
    // v12.0.0: Service locator for IValueResolver instantiation
    private readonly Func<Type, object>? _serviceLocator;

    // v12.0.0: Cache for compiled resolver delegates
    private static readonly ConcurrentDictionary<Type, Func<object, object, object, object?, ResolutionContext, object?>>
        _resolverDelegateCache = new();

    internal Mapper(TypeMapRegistry registry)
        : this(registry, new Dictionary<(Type, Type), Delegate>(), null)
    {
    }

    internal Mapper(TypeMapRegistry registry, Dictionary<(Type, Type), Delegate> generatedPlans)
        : this(registry, generatedPlans, null)
    {
    }

    internal Mapper(TypeMapRegistry registry, Dictionary<(Type, Type), Delegate> generatedPlans, Func<Type, object>? serviceLocator)
    {
        _registry = registry;
        _generatedPlans = generatedPlans;
        _context = new ResolutionContext(this);
        _serviceLocator = serviceLocator;
    }

    public TDestination Map<TDestination>(object source)
    {
        if (source == null)
            return default!;

        var sourceType = source.GetType();
        var destType = typeof(TDestination);

        return (TDestination)MapInternal(source, sourceType, destType)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source == null)
            return default!;

        // v8.0.0: FAST PATH - Use generic static cache for generated mappers
        // After first call, this is just a static field read (~1ns) + direct typed delegate call
        var cachedMapper = GeneratedMapperCache<TSource, TDestination>.GetMapper(_generatedPlans);
        if (cachedMapper != null)
            return cachedMapper(source)!;

        // Standard path for non-generated mappings
        return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination))!;
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null)
            return destination;

        MapToExisting(source, destination!, typeof(TSource), typeof(TDestination));
        return destination;
    }

    public object Map(object source, Type sourceType, Type destinationType)
    {
        return MapInternal(source, sourceType, destinationType)!;
    }

    public object Map(object source, object destination, Type sourceType, Type destinationType)
    {
        MapToExisting(source, destination, sourceType, destinationType);
        return destination;
    }

    private object? MapInternal(object? source, Type sourceType, Type destType)
    {
        if (source == null)
            return null;

        var actualSourceType = source.GetType();

        // Get TypeMap early for v8.0.0 features (MaxDepth, PreserveReferences)
        var typeMap = _registry.FindTypeMap(actualSourceType, destType);

        // v8.0.0: PreserveReferences - check if we've already mapped this object
        if (typeMap?.PreserveReferences == true && !actualSourceType.IsValueType)
        {
            if (_context.TryGetMappedObject(source, destType, out var existingMapped))
            {
                return existingMapped;
            }
        }

        // v8.0.0: MaxDepth - check if we've exceeded depth for this type pair
        if (typeMap?.MaxDepth != null)
        {
            if (!_context.ShouldMapNested(actualSourceType, destType, typeMap.MaxDepth))
            {
                return null; // Depth limit reached
            }
            _context.IncrementDepth(actualSourceType, destType);
        }

        try
        {
            return MapInternalCore(source, sourceType, destType, actualSourceType, typeMap);
        }
        finally
        {
            // v8.0.0: Decrement depth after mapping completes
            if (typeMap?.MaxDepth != null)
            {
                _context.DecrementDepth(actualSourceType, destType);
            }
        }
    }

    private object? MapInternalCore(object source, Type sourceType, Type destType, Type actualSourceType, TypeMap? typeMap)
    {
        // GENERATED PATH (v6.0.0) - Highest priority for compile-time generated mappers
        // v8.0.0: Uses typed delegates stored directly (no wrapper lambda overhead)
        // Note: The fast typed path is in Map<TSource, TDest>() which bypasses this entirely
        if (_generatedPlans.TryGetValue((actualSourceType, destType), out var generatedPlan))
        {
            // Cast to typed delegate and invoke (still faster than wrapper lambda)
            return generatedPlan.DynamicInvoke(source);
        }

        // Use cached type analysis to avoid repeated reflection
        var destAnalysis = ReflectionCache.GetTypeAnalysis(destType);

        // Handle nullable struct destination (e.g., Point? as destination)
        if (destAnalysis.UnderlyingNullableType != null &&
            destAnalysis.UnderlyingNullableType.IsValueType &&
            !ReflectionCache.GetTypeAnalysis(destAnalysis.UnderlyingNullableType).IsSimple)
        {
            // Map to the underlying struct type, result will be boxed and can be assigned to Nullable<T>
            return MapInternal(source, actualSourceType, destAnalysis.UnderlyingNullableType);
        }

        // Handle collections - check destination type first
        if (destAnalysis.IsEnumerable)
        {
            var sourceAnalysis = ReflectionCache.GetTypeAnalysis(actualSourceType);
            if (sourceAnalysis.IsEnumerable)
            {
                return MapCollection(source, sourceAnalysis.EnumerableElementType!, destAnalysis.EnumerableElementType!, destType);
            }
        }

        // v8.0.0: Check if this is a polymorphic mapping (Include) where destination type differs
        var isPolymorphicMapping = typeMap != null && typeMap.DestinationType != destType;

        // FAST PATH: Use pre-compiled execution plan if available
        // Skip if polymorphic mapping - we need to create the derived destination type
        if (!isPolymorphicMapping && typeMap?.ExecutionPlan != null)
        {
            var result = typeMap.ExecutionPlan(source);

            // HYBRID EXECUTION: If there are collection properties, map them via legacy path
            if (typeMap.CollectionProperties != null && typeMap.CollectionProperties.Count > 0)
            {
                ApplyCollectionMappingOnly(source, result!, actualSourceType, destType, typeMap);
            }

            return result;
        }

        // Try convention-based execution plan for types without explicit mapping
        // Skip if we have a polymorphic mapping that needs special handling
        if (typeMap == null && !isPolymorphicMapping && !destType.IsValueType)
        {
            var conventionPlan = _registry.GetConventionPlan(actualSourceType, destType);
            if (conventionPlan != null)
            {
                return conventionPlan(source);
            }
        }

        // LEGACY PATH: Fall back to runtime reflection for complex cases
        return MapInternalLegacy(source, actualSourceType, destType, typeMap);
    }

    /// <summary>
    /// Legacy mapping path using runtime reflection.
    /// Used for complex cases like converters, value types, or when execution plan isn't available.
    /// </summary>
    private object? MapInternalLegacy(object source, Type actualSourceType, Type destType, TypeMap? typeMap)
    {
        // Use lambda converter if defined (v7.0.0)
        if (typeMap?.LambdaConverter != null)
        {
            return typeMap.LambdaConverter.DynamicInvoke(source);
        }

        // Use converter if defined
        if (typeMap?.Converter != null || typeMap?.ConverterType != null)
        {
            return ExecuteConverter(source, actualSourceType, destType, typeMap);
        }

        // v8.0.0: For polymorphic mapping (Include), use the TypeMap's destination type
        // e.g., when mapping Car as Vehicle -> VehicleDto, if we found Car -> CarDto TypeMap,
        // we should create CarDto, not VehicleDto
        // But for open generic TypeMaps, we need to use the requested destType (which is closed)
        var actualDestType = destType;
        if (typeMap != null && typeMap.DestinationType != destType && !typeMap.IsOpenGeneric)
        {
            actualDestType = typeMap.DestinationType;
        }

        // v8.0.0: Find base TypeMap if this is a polymorphic mapping via Include
        // The base TypeMap is the one that has the Include() for this derived type
        TypeMap? baseTypeMap = null;
        if (typeMap != null && actualDestType != destType)
        {
            // This is a polymorphic mapping - find the base that included us
            baseTypeMap = FindBaseTypeMapForInclude(typeMap.SourceType, typeMap.DestinationType);
        }

        // v8.0.0: Create instance using custom constructor if configured, otherwise default
        var destination = CreateInstanceWithConstructor(source, actualDestType, typeMap);

        // v8.0.0: Track for PreserveReferences before populating (to handle circular refs)
        if (typeMap?.PreserveReferences == true && !actualSourceType.IsValueType)
        {
            _context.TrackMappedObject(source, destination);
        }

        // v8.0.0: Execute BeforeMap - first base (if polymorphic), then derived
        if (baseTypeMap != null)
        {
            ExecuteBeforeMap(source, destination, baseTypeMap);
        }
        ExecuteBeforeMap(source, destination, typeMap);

        // For value types, we need to capture returned value because struct modifications
        // on boxed value don't propagate
        if (actualDestType.IsValueType)
        {
            if (typeMap != null)
            {
                destination = ApplyMemberMapsForValueType(source, destination, typeMap);
            }
            destination = ApplyConventionMappingForValueType(source, destination, actualSourceType, actualDestType, typeMap);
            // v10.0.0: Apply destination-dependent mappings AFTER convention mapping for value types
            if (typeMap != null)
            {
                destination = ApplyDestinationDependentMemberMapsForValueType(source, destination, typeMap);
            }
        }
        else
        {
            // v8.0.0: Collect all base TypeMaps for proper inheritance
            var allBaseTypeMaps = CollectAllBaseTypeMaps(typeMap, baseTypeMap);

            // v8.0.0: Apply IncludeBase configuration first (if any)
            ApplyIncludeBaseConfiguration(source, destination, typeMap);

            // v8.0.0: Apply base TypeMap's member maps (if polymorphic Include)
            if (baseTypeMap != null)
            {
                ApplyBaseMemberMaps(source, destination, baseTypeMap, typeMap!);
            }

            if (typeMap != null)
            {
                ApplyMemberMaps(source, destination, typeMap);
            }
            // v8.0.0: Pass all base TypeMaps to skip members configured in any base
            ApplyConventionMappingWithBases(source, destination, actualSourceType, actualDestType, typeMap, allBaseTypeMaps);

            // v10.0.0: Apply destination-dependent mappings AFTER convention mapping
            // This ensures that destination properties are fully populated when MapFrom((src, dest) => ...) executes
            if (typeMap != null)
            {
                ApplyDestinationDependentMemberMaps(source, destination, typeMap);
            }

            // v8.0.0: Apply ForPath mappings (after member maps, before AfterMap)
            if (typeMap != null)
            {
                ApplyPathMaps(source, destination, typeMap);
            }
        }

        // v8.0.0: Execute AfterMap - first derived, then base (if polymorphic)
        ExecuteAfterMap(source, destination, typeMap);
        if (baseTypeMap != null)
        {
            ExecuteAfterMap(source, destination, baseTypeMap);
        }

        return destination;
    }

    /// <summary>
    /// Finds the base TypeMap that has Include() for the given derived source/dest types.
    /// </summary>
    private TypeMap? FindBaseTypeMapForInclude(Type derivedSource, Type derivedDest)
    {
        // Look for a TypeMap where IncludedDerivedTypes contains our derived types
        foreach (var (key, baseMap) in GetAllTypeMaps())
        {
            foreach (var (includedSource, includedDest) in baseMap.IncludedDerivedTypes)
            {
                if (includedSource == derivedSource && includedDest == derivedDest)
                {
                    return baseMap;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all TypeMaps from the registry for iteration.
    /// </summary>
    private IEnumerable<((Type, Type), TypeMap)> GetAllTypeMaps()
    {
        // We need to access the registry's type maps
        // This is a workaround - ideally the registry would expose this
        return _registry.GetAllTypeMapsForIteration();
    }

    /// <summary>
    /// Collects all base TypeMaps from both Include (polymorphic) and IncludeBase chains.
    /// </summary>
    private List<TypeMap> CollectAllBaseTypeMaps(TypeMap? typeMap, TypeMap? includeBaseTypeMap)
    {
        var result = new List<TypeMap>();

        // Add the Include base (polymorphic dispatch)
        if (includeBaseTypeMap != null)
        {
            result.Add(includeBaseTypeMap);
        }

        // Walk the IncludeBase chain
        var current = typeMap;
        while (current?.IncludedBaseType != null)
        {
            var (baseSourceType, baseDestType) = current.IncludedBaseType.Value;
            var baseMap = _registry.FindTypeMap(baseSourceType, baseDestType);
            if (baseMap != null)
            {
                result.Add(baseMap);
            }
            current = baseMap;
        }

        return result;
    }

    /// <summary>
    /// Applies convention mapping while skipping members configured in any base TypeMap.
    /// </summary>
    private void ApplyConventionMappingWithBases(object source, object destination,
        Type sourceType, Type destType, TypeMap? typeMap, List<TypeMap> baseTypeMaps)
    {
        // Use cached PropertyInfo arrays and dictionaries
        var destProps = ReflectionCache.GetWritableProperties(destType);
        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);

        // Use pre-computed member sets from TypeMap (no allocations)
        var configuredMembers = typeMap?.ConfiguredMembers;
        var ignoredMembers = typeMap?.IgnoredMembers;

        foreach (var destProp in destProps)
        {
            // Skip if explicitly configured (either mapped or ignored)
            if (configuredMembers != null && configuredMembers.Contains(destProp.Name))
                continue;
            if (ignoredMembers != null && ignoredMembers.Contains(destProp.Name))
                continue;

            // v8.0.0: Also skip if configured in any base TypeMap
            var skipDueToBase = false;
            foreach (var baseMap in baseTypeMaps)
            {
                if (baseMap.ConfiguredMembers?.Contains(destProp.Name) == true ||
                    baseMap.IgnoredMembers?.Contains(destProp.Name) == true)
                {
                    skipDueToBase = true;
                    break;
                }
            }
            if (skipDueToBase)
                continue;

            // Try exact match first (case-sensitive), then fall back to case-insensitive
            if (!sourcePropsExact.TryGetValue(destProp.Name, out var sourceProp))
            {
                sourcePropsCI.TryGetValue(destProp.Name, out sourceProp);
            }

            if (sourceProp != null)
            {
                try
                {
                    var sourceValue = ReflectionCache.GetValue(sourceProp, source);

                    // v8.0.0: Apply transforms
                    var transformedValue = ApplyTransforms(sourceValue, typeMap, destProp.PropertyType);

                    if (transformedValue != null)
                    {
                        var convertedValue = ConvertValue(transformedValue, sourceProp.PropertyType, destProp.PropertyType);
                        ReflectionCache.SetValue(destProp, destination, convertedValue);
                    }
                    else
                    {
                        // v12.1.0: Check for type converter first - it may want to handle null -> null mapping
                        var converterTypeMap = _registry.FindTypeMap(sourceProp.PropertyType, destProp.PropertyType);
                        if (converterTypeMap != null && (converterTypeMap.Converter != null || converterTypeMap.ConverterType != null))
                        {
                            // Let the converter handle null - it decides what to return
                            // Pass null as source - the converter should handle it
                            var convertedValue = ExecuteConverter(null!, sourceProp.PropertyType, destProp.PropertyType, converterTypeMap);
                            ReflectionCache.SetValue(destProp, destination, convertedValue);
                        }
                        // Handle null values for dictionaries and collections
                        else
                        {
                            var destPropAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);
                            if (destPropAnalysis.IsDictionary)
                            {
                                var emptyDict = CreateEmptyDictionary(destProp.PropertyType, destPropAnalysis.DictionaryKeyType!, destPropAnalysis.DictionaryValueType!);
                                ReflectionCache.SetValue(destProp, destination, emptyDict);
                            }
                            else if (destPropAnalysis.IsCollection)
                            {
                                var emptyCollection = CreateEmptyCollection(destProp.PropertyType, destPropAnalysis.CollectionElementType!);
                                ReflectionCache.SetValue(destProp, destination, emptyCollection);
                            }
                            else if (destPropAnalysis.IsNullable || !destProp.PropertyType.IsValueType)
                            {
                                ReflectionCache.SetValue(destProp, destination, null);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore mapping errors for convention mapping
                }
            }
        }

        // v8.0.0: Apply IncludedMembers flattening
        ApplyIncludedMembers(source, destination, destType, typeMap);
    }

    private object CreateInstance(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return CreateInstanceInternal(underlyingType);
        }

        return CreateInstanceInternal(type);
    }

    /// <summary>
    /// v8.0.0: Create instance using custom constructor if configured via ConstructUsing() or ForCtorParam(),
    /// otherwise falls back to default construction.
    /// </summary>
    private object CreateInstanceWithConstructor(object source, Type destType, TypeMap? typeMap)
    {
        // v8.0.0: Check for ConstructUsing with ResolutionContext first
        if (typeMap?.ConstructUsingWithContext != null)
        {
            var constructor = (Func<object, ResolutionContext, object>)typeMap.ConstructUsingWithContext;
            return constructor(source, _context);
        }

        // v8.0.0: Check for ConstructUsing without context
        if (typeMap?.ConstructUsing != null)
        {
            var constructor = (Func<object, object>)typeMap.ConstructUsing;
            return constructor(source);
        }

        // v8.0.0: Check for ForCtorParam configuration
        if (typeMap?.CtorParamMaps != null && typeMap.CtorParamMaps.Count > 0)
        {
            return CreateInstanceWithCtorParams(source, destType, typeMap);
        }

        // Default: use standard instance creation
        return CreateInstance(destType);
    }

    /// <summary>
    /// v8.0.0: Create instance using ForCtorParam() configured constructor parameters.
    /// </summary>
    private object CreateInstanceWithCtorParams(object source, Type destType, TypeMap typeMap)
    {
        // Get all constructors sorted by parameter count (descending) to find best match
        var constructors = destType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToArray();

        if (constructors.Length == 0)
        {
            throw new InvalidOperationException(
                $"No public constructors found for type '{destType.Name}' when using ForCtorParam().");
        }

        // Build a dictionary of configured parameter values
        var configuredParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var ctorParamMap in typeMap.CtorParamMaps)
        {
            if (ctorParamMap.SourceValueResolver != null)
            {
                var value = ctorParamMap.SourceValueResolver.DynamicInvoke(source);
                configuredParams[ctorParamMap.ParameterName] = value;
            }
        }

        // Find constructor that can be satisfied by our configured parameters + source properties
        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            var canUse = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                // First check if we have an explicit ForCtorParam mapping
                if (configuredParams.TryGetValue(param.Name!, out var configuredValue))
                {
                    args[i] = ConvertValue(configuredValue, param.ParameterType);
                }
                // Then check if source has a matching property (convention)
                else
                {
                    var sourceProp = source.GetType().GetProperty(param.Name!,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (sourceProp != null && sourceProp.CanRead)
                    {
                        args[i] = ConvertValue(sourceProp.GetValue(source), param.ParameterType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else if (param.ParameterType.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(param.ParameterType);
                    }
                    else
                    {
                        // Can't satisfy this parameter
                        canUse = false;
                        break;
                    }
                }
            }

            if (canUse)
            {
                return ctor.Invoke(args);
            }
        }

        // Fallback: use parameterless constructor if available
        var defaultCtor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
        if (defaultCtor != null)
        {
            return defaultCtor.Invoke(Array.Empty<object>());
        }

        throw new InvalidOperationException(
            $"Could not find a suitable constructor for type '{destType.Name}'. " +
            "Configure ForCtorParam() for all required constructor parameters.");
    }

    /// <summary>
    /// Converts a value to the target type, handling nulls and basic conversions.
    /// </summary>
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType))
        {
            return value;
        }

        // Try conversion
        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return value;
        }
    }

    private static object CreateInstanceInternal(Type type)
    {
        return ReflectionCache.CreateInstance(type);
    }

    private object MapCollection(object source, Type sourceElementType, Type destElementType, Type destCollectionType)
    {
        var sourceEnumerable = (IEnumerable)source;
        var sourceType = source.GetType();

        // FAST PATH: Try to use pre-compiled collection execution plan (v4.3.0)
        var collectionPlan = _registry.GetCollectionPlan(
            sourceType, destCollectionType, sourceElementType, destElementType);

        if (collectionPlan != null)
        {
            return collectionPlan(source);
        }

        // LEGACY PATH: Runtime iteration with MapInternal per element
        // (used for complex elements that can't be inlined)

        // v5.0.0: Use ArrayPool to reduce heap allocations
        int sourceCount = GetCollectionCount(source);

        // Handle empty collection
        if (sourceCount == 0)
        {
            return CreateEmptyCollectionOfType(destCollectionType, destElementType);
        }

        // Use cached type analysis
        var sourceElemAnalysis = ReflectionCache.GetTypeAnalysis(sourceElementType);
        var destElemAnalysis = ReflectionCache.GetTypeAnalysis(destElementType);
        var isPrimitiveCollection = sourceElemAnalysis.IsSimple && destElemAnalysis.IsSimple;

        // If count is known, use ArrayPool (v5.0.0 optimization)
        if (sourceCount > 0)
        {
            var rentedArray = ArrayPool<object?>.Shared.Rent(sourceCount);
            try
            {
                int index = 0;
                foreach (var item in sourceEnumerable)
                {
                    if (item == null)
                    {
                        rentedArray[index++] = null;
                    }
                    else if (isPrimitiveCollection)
                    {
                        rentedArray[index++] = ConvertPrimitiveValue(item, destElementType);
                    }
                    else
                    {
                        rentedArray[index++] = MapInternal(item, sourceElementType, destElementType);
                    }
                }

                // Create the destination collection from the pooled array
                return CreateCollectionFromArray(destCollectionType, destElementType, rentedArray, index);
            }
            finally
            {
                // Return array to pool, clearing references to allow GC
                ArrayPool<object?>.Shared.Return(rentedArray, clearArray: true);
            }
        }

        // Fallback for unknown count: use List<object?> (pre-v5.0.0 behavior)
        var mappedItems = new List<object?>();
        foreach (var item in sourceEnumerable)
        {
            if (item == null)
            {
                mappedItems.Add(null);
            }
            else if (isPrimitiveCollection)
            {
                mappedItems.Add(ConvertPrimitiveValue(item, destElementType));
            }
            else
            {
                mappedItems.Add(MapInternal(item, sourceElementType, destElementType));
            }
        }

        return CreateCollectionOfType(destCollectionType, destElementType, mappedItems);
    }

    /// <summary>
    /// Gets the count of items in a collection without enumerating (v5.0.0).
    /// Returns -1 if count cannot be determined without enumeration.
    /// </summary>
    private static int GetCollectionCount(object source)
    {
        // ICollection covers List<T>, arrays, HashSet<T>, Dictionary<K,V>, etc.
        if (source is ICollection collection)
            return collection.Count;

        return -1;  // Cannot determine count without enumeration
    }

    /// <summary>
    /// Creates an empty collection of the specified destination type (v5.0.0).
    /// </summary>
    private static object CreateEmptyCollectionOfType(Type collectionType, Type elementType)
    {
        if (collectionType.IsArray)
        {
            return Array.CreateInstance(elementType, 0);
        }

        // For List<T> and interface types, create empty List<T>
        var listType = typeof(List<>).MakeGenericType(elementType);
        return ReflectionCache.CreateInstance(listType);
    }

    /// <summary>
    /// Creates a collection from a pooled array without intermediate List allocation (v5.0.0).
    /// </summary>
    private static object CreateCollectionFromArray(Type collectionType, Type elementType,
        object?[] items, int count)
    {
        // Array destination
        if (collectionType.IsArray)
        {
            var array = Array.CreateInstance(elementType, count);
            for (int i = 0; i < count; i++)
            {
                array.SetValue(items[i], i);
            }
            return array;
        }

        // List<T> and interface types (IList<T>, ICollection<T>, IEnumerable<T>, etc.)
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
                var list = (IList)Activator.CreateInstance(listType, count)!;
                for (int i = 0; i < count; i++)
                {
                    list.Add(items[i]);
                }
                return list;
            }
        }

        // Fallback: use CollectionFactory for other types (HashSet, Queue, Stack, etc.)
        var itemsList = new List<object?>(count);
        for (int i = 0; i < count; i++)
        {
            itemsList.Add(items[i]);
        }
        return CreateCollectionOfType(collectionType, elementType, itemsList);
    }

    private static object CreateCollectionOfType(Type collectionType, Type elementType, List<object?> items)
    {
        // Use optimized CollectionFactory - eliminates MethodInfo.Invoke in loops
        var builder = CollectionFactory.GetBuilder(collectionType, elementType);
        return builder.BuildFromItems(items);
    }

    /// <summary>
    /// Converts a primitive/simple value directly without trying to create a new instance.
    /// </summary>
    private object? ConvertPrimitiveValue(object value, Type destType)
    {
        if (value == null)
            return null;

        var sourceType = value.GetType();

        // Same type - return as is
        if (destType == sourceType || destType.IsAssignableFrom(sourceType))
            return value;

        // Handle nullable destination
        var underlyingDestType = Nullable.GetUnderlyingType(destType);
        if (underlyingDestType != null)
        {
            return ConvertPrimitiveValue(value, underlyingDestType);
        }

        // String - just return as is (strings are immutable, no need to create new)
        if (destType == typeof(string) && sourceType == typeof(string))
            return value;

        // Convert to string
        if (destType == typeof(string))
            return value.ToString();

        // String to enum conversion
        if (destType.IsEnum && sourceType == typeof(string))
        {
            var stringValue = (string)value;
            if (Enum.TryParse(destType, stringValue, ignoreCase: true, out var enumResult))
                return enumResult;
            return Enum.ToObject(destType, 0); // Default value
        }

        // Enum conversions
        if (destType.IsEnum && IsNumericType(sourceType))
        {
            return Enum.ToObject(destType, value);
        }

        if (sourceType.IsEnum && IsNumericType(destType))
        {
            return Convert.ChangeType(Convert.ToInt64(value), destType);
        }

        if (sourceType.IsEnum && destType == typeof(string))
        {
            return value.ToString();
        }

        // Standard conversion
        try
        {
            return Convert.ChangeType(value, destType);
        }
        catch
        {
            return value;
        }
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
               type == typeof(ushort) || type == typeof(sbyte);
    }

    /// <summary>
    /// v8.0.0: Apply type-based transforms to a value.
    /// </summary>
    private object? ApplyTransforms(object? value, TypeMap? typeMap)
    {
        if (typeMap == null)
            return value;

        // For null values, check if there's a string transform that handles null
        Type? valueType = value?.GetType();
        if (valueType == null)
        {
            // For null values, try string transform (most common null-handling scenario)
            var stringTransform = typeMap.GetTransform(typeof(string));
            if (stringTransform != null)
            {
                try
                {
                    var compiled = stringTransform.Compile();
                    // Must pass null as array element, not as array itself
                    return compiled.DynamicInvoke(new object?[] { null });
                }
                catch
                {
                    return value;
                }
            }
            return value;
        }

        var transform = typeMap.GetTransform(valueType);
        if (transform == null)
            return value;

        try
        {
            // Compile and invoke the transform expression
            var compiled = transform.Compile();
            return compiled.DynamicInvoke(value);
        }
        catch
        {
            // If transform fails, return original value
            return value;
        }
    }

    /// <summary>
    /// v8.0.0: Apply type-based transforms to a value with destination type hint.
    /// Used for null values where we need to know the target property type.
    /// </summary>
    private object? ApplyTransforms(object? value, TypeMap? typeMap, Type destPropertyType)
    {
        if (typeMap == null)
            return value;

        // For non-null values, use value's type
        if (value != null)
        {
            var valueType = value.GetType();
            var transform = typeMap.GetTransform(valueType);
            if (transform != null)
            {
                try
                {
                    var compiled = transform.Compile();
                    return compiled.DynamicInvoke(value);
                }
                catch
                {
                    return value;
                }
            }
            return value;
        }

        // For null values, use destination property type to find transform
        var nullTransform = typeMap.GetTransform(destPropertyType);
        if (nullTransform != null)
        {
            try
            {
                var compiled = nullTransform.Compile();
                // Must pass null as array element, not as array itself
                return compiled.DynamicInvoke(new object?[] { null });
            }
            catch
            {
                return value;
            }
        }
        return value;
    }

    private void ApplyMemberMaps(object source, object destination, TypeMap typeMap)
    {
        var destType = destination.GetType();
        var destPropsDict = ReflectionCache.GetWritablePropertiesDict(destType);

        foreach (var memberMap in typeMap.MemberMaps)
        {
            if (memberMap.Ignored)
                continue;

            // v10.0.0: Skip destination-dependent mappings here - they're applied later
            // in ApplyDestinationDependentMemberMaps after convention mapping
            if (memberMap.HasDestinationParameter)
                continue;

            ApplySingleMemberMap(source, destination, memberMap, destPropsDict, typeMap);
        }
    }

    /// <summary>
    /// v10.0.0: Applies destination-dependent member mappings.
    /// Called AFTER convention mapping so the destination is fully populated.
    /// </summary>
    private void ApplyDestinationDependentMemberMaps(object source, object destination, TypeMap typeMap)
    {
        var destType = destination.GetType();
        var destPropsDict = ReflectionCache.GetWritablePropertiesDict(destType);

        foreach (var memberMap in typeMap.MemberMaps)
        {
            if (memberMap.Ignored)
                continue;

            // Only apply destination-dependent mappings
            if (!memberMap.HasDestinationParameter)
                continue;

            ApplyDestinationDependentMemberMap(source, destination, memberMap, destPropsDict, typeMap);
        }
    }

    /// <summary>
    /// Applies a single regular member map (without destination dependency).
    /// </summary>
    private void ApplySingleMemberMap(object source, object destination, MemberMap memberMap,
        Dictionary<string, System.Reflection.PropertyInfo> destPropsDict, TypeMap typeMap)
    {
        // v8.0.0: PreCondition evaluated BEFORE value resolution
        if (memberMap.PreCondition != null && !memberMap.PreCondition(source))
            return;

        if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destProp))
            return;

        object? value = null;

        // v12.0.0: IValueResolver takes priority
        if (memberMap.HasValueResolver)
        {
            try
            {
                var currentDestValue = ReflectionCache.GetValue(destProp, destination);
                value = InvokeValueResolver(memberMap, source, destination, currentDestValue);
            }
            catch
            {
                // If the resolver fails, skip this member
                return;
            }
        }
        // Use CompiledResolver to avoid DynamicInvoke overhead
        else if (memberMap.CompiledResolver != null)
        {
            try
            {
                value = memberMap.CompiledResolver(source);
            }
            catch
            {
                // If the resolver fails, skip this member
                return;
            }
        }
        else if (memberMap.SourceValueResolver != null)
        {
            // Fallback to DynamicInvoke for backward compatibility
            try
            {
                value = memberMap.SourceValueResolver.DynamicInvoke(source);
            }
            catch
            {
                return;
            }
        }

        // v8.0.0: Apply NullSubstitute if value is null
        if (value == null && memberMap.HasNullSubstitute)
        {
            value = memberMap.NullSubstitute;
        }

        // v8.0.0: Apply transforms (with destination property type for null handling)
        value = ApplyTransforms(value, typeMap, destProp.PropertyType);

        // v8.0.0: Condition evaluated AFTER value resolution (and transforms)
        if (memberMap.ConditionWithContext != null)
        {
            if (!memberMap.ConditionWithContext(source, destination, value, _context))
                return;
        }
        else if (memberMap.Condition != null)
        {
            if (!memberMap.Condition(source, destination, value))
                return;
        }

        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, destProp.PropertyType);
            ReflectionCache.SetValue(destProp, destination, convertedValue);
        }
    }

    /// <summary>
    /// v10.0.0: Applies a destination-dependent member map.
    /// Called AFTER regular mappings, so destination is already populated.
    /// </summary>
    private void ApplyDestinationDependentMemberMap(object source, object destination, MemberMap memberMap,
        Dictionary<string, System.Reflection.PropertyInfo> destPropsDict, TypeMap typeMap)
    {
        // v8.0.0: PreCondition evaluated BEFORE value resolution
        if (memberMap.PreCondition != null && !memberMap.PreCondition(source))
            return;

        if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destProp))
            return;

        object? value = null;

        // v10.0.0: Use DestinationResolver which passes the actual destination
        if (memberMap.DestinationResolver != null)
        {
            try
            {
                value = memberMap.DestinationResolver(source, destination);
            }
            catch
            {
                // If the resolver fails, skip this member
                return;
            }
        }
        else if (memberMap.CompiledResolver != null)
        {
            // Fallback to regular resolver (shouldn't happen for destination-dependent mappings)
            try
            {
                value = memberMap.CompiledResolver(source);
            }
            catch
            {
                return;
            }
        }

        // v8.0.0: Apply NullSubstitute if value is null
        if (value == null && memberMap.HasNullSubstitute)
        {
            value = memberMap.NullSubstitute;
        }

        // v8.0.0: Apply transforms (with destination property type for null handling)
        value = ApplyTransforms(value, typeMap, destProp.PropertyType);

        // v8.0.0: Condition evaluated AFTER value resolution (and transforms)
        if (memberMap.ConditionWithContext != null)
        {
            if (!memberMap.ConditionWithContext(source, destination, value, _context))
                return;
        }
        else if (memberMap.Condition != null)
        {
            if (!memberMap.Condition(source, destination, value))
                return;
        }

        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, destProp.PropertyType);
            ReflectionCache.SetValue(destProp, destination, convertedValue);
        }
    }

    private void ApplyConventionMapping(object source, object destination,
        Type sourceType, Type destType, TypeMap? typeMap, TypeMap? baseTypeMap = null)
    {
        // Use cached PropertyInfo arrays and dictionaries
        var destProps = ReflectionCache.GetWritableProperties(destType);
        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);

        // Use pre-computed member sets from TypeMap (no allocations)
        var configuredMembers = typeMap?.ConfiguredMembers;
        var ignoredMembers = typeMap?.IgnoredMembers;

        // v8.0.0: Also track configured members from base TypeMap (for Include)
        var baseConfiguredMembers = baseTypeMap?.ConfiguredMembers;
        var baseIgnoredMembers = baseTypeMap?.IgnoredMembers;

        foreach (var destProp in destProps)
        {
            // Skip if explicitly configured (either mapped or ignored)
            if (configuredMembers != null && configuredMembers.Contains(destProp.Name))
                continue;
            if (ignoredMembers != null && ignoredMembers.Contains(destProp.Name))
                continue;

            // v8.0.0: Also skip if configured in base TypeMap (for Include inheritance)
            if (baseConfiguredMembers != null && baseConfiguredMembers.Contains(destProp.Name))
                continue;
            if (baseIgnoredMembers != null && baseIgnoredMembers.Contains(destProp.Name))
                continue;

            // Try exact match first (case-sensitive), then fall back to case-insensitive
            if (!sourcePropsExact.TryGetValue(destProp.Name, out var sourceProp))
            {
                sourcePropsCI.TryGetValue(destProp.Name, out sourceProp);
            }

            if (sourceProp != null)
            {
                try
                {
                    var sourceValue = ReflectionCache.GetValue(sourceProp, source);

                    // v8.0.0: Apply transforms (before null check - transform might convert null to a value)
                    // Use destination property type hint for null values
                    var transformedValue = ApplyTransforms(sourceValue, typeMap, destProp.PropertyType);

                    if (transformedValue != null)
                    {
                        var convertedValue = ConvertValue(transformedValue, sourceProp.PropertyType, destProp.PropertyType);
                        ReflectionCache.SetValue(destProp, destination, convertedValue);
                    }
                    else
                    {
                        // v12.1.0: Check for type converter first - it may want to handle null -> null mapping
                        var converterTypeMap = _registry.FindTypeMap(sourceProp.PropertyType, destProp.PropertyType);
                        if (converterTypeMap != null && (converterTypeMap.Converter != null || converterTypeMap.ConverterType != null))
                        {
                            // Let the converter handle null - it decides what to return
                            // Pass null as source - the converter should handle it
                            var convertedValue = ExecuteConverter(null!, sourceProp.PropertyType, destProp.PropertyType, converterTypeMap);
                            ReflectionCache.SetValue(destProp, destination, convertedValue);
                        }
                        else
                        {
                            // Use cached type analysis for null value handling
                            var destPropAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);
                            if (destPropAnalysis.IsDictionary)
                            {
                                // Null source dictionary -> empty destination dictionary
                                var emptyDict = CreateEmptyDictionary(destProp.PropertyType, destPropAnalysis.DictionaryKeyType!, destPropAnalysis.DictionaryValueType!);
                                ReflectionCache.SetValue(destProp, destination, emptyDict);
                            }
                            else if (destPropAnalysis.IsCollection)
                            {
                                // Null source collection -> empty destination collection
                                var emptyCollection = CreateEmptyCollection(destProp.PropertyType, destPropAnalysis.CollectionElementType!);
                                ReflectionCache.SetValue(destProp, destination, emptyCollection);
                            }
                            else if (destPropAnalysis.IsNullable || !destProp.PropertyType.IsValueType)
                            {
                                ReflectionCache.SetValue(destProp, destination, null);
                            }
                        }
                    }
                }
                catch
                {
                    // Skip properties that can't be mapped
                }
            }
        }

        // v8.0.0: Apply IncludeMembers - flatten properties from nested source objects
        ApplyIncludedMembers(source, destination, destType, typeMap);
    }

    /// <summary>
    /// v8.0.0: Applies IncludeMembers flattening - maps properties from nested source objects
    /// to destination properties that don't have a direct source match.
    /// </summary>
    private void ApplyIncludedMembers(object source, object destination, Type destType, TypeMap? typeMap)
    {
        if (typeMap?.IncludedMembers == null || typeMap.IncludedMembers.Count == 0)
            return;

        var sourceType = source.GetType();
        var destProps = ReflectionCache.GetWritableProperties(destType);
        var destPropsDict = ReflectionCache.GetWritablePropertiesDict(destType);
        var configuredMembers = typeMap.ConfiguredMembers;
        var ignoredMembers = typeMap.IgnoredMembers;

        // v10.0.0: Get source properties to check which dest props have a direct source match
        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);

        foreach (var includedMemberExpr in typeMap.IncludedMembers)
        {
            // Get the nested source object
            object? nestedSource;
            try
            {
                var compiled = includedMemberExpr.Compile();
                nestedSource = compiled.DynamicInvoke(source);
            }
            catch
            {
                continue;
            }

            if (nestedSource == null)
                continue;

            var nestedSourceType = nestedSource.GetType();
            var nestedSourceProps = ReflectionCache.GetReadablePropertiesDict(nestedSourceType);
            var nestedSourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(nestedSourceType);

            // Map properties from nested source to destination
            foreach (var destProp in destProps)
            {
                // Skip if explicitly configured
                if (configuredMembers != null && configuredMembers.Contains(destProp.Name))
                    continue;
                if (ignoredMembers != null && ignoredMembers.Contains(destProp.Name))
                    continue;

                // v10.0.0: Skip if source has a direct matching property (source takes precedence over IncludeMembers)
                // This is the correct behavior: IncludeMembers only fills in properties not mapped from primary source
                if (sourcePropsExact.ContainsKey(destProp.Name) || sourcePropsCI.ContainsKey(destProp.Name))
                    continue;

                // Try to find matching property in nested source
                if (!nestedSourceProps.TryGetValue(destProp.Name, out var nestedProp))
                {
                    nestedSourcePropsCI.TryGetValue(destProp.Name, out nestedProp);
                }

                if (nestedProp != null)
                {
                    try
                    {
                        var nestedValue = ReflectionCache.GetValue(nestedProp, nestedSource);
                        if (nestedValue != null)
                        {
                            var convertedValue = ConvertValue(nestedValue, nestedProp.PropertyType, destProp.PropertyType);
                            ReflectionCache.SetValue(destProp, destination, convertedValue);
                        }
                        else
                        {
                            // v10.0.0: For null values, set destination to null (if nullable)
                            var destPropAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);
                            if (destPropAnalysis.IsNullable || !destProp.PropertyType.IsValueType)
                            {
                                ReflectionCache.SetValue(destProp, destination, null);
                            }
                        }
                    }
                    catch
                    {
                        // Skip if conversion fails
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a value is the default value for its type.
    /// </summary>
    private static bool IsDefaultValue(object value, Type type)
    {
        if (value == null)
            return true;

        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }

        // For reference types, only null is default
        return false;
    }

    /// <summary>
    /// Applies mapping only for collection properties after execution plan (hybrid execution).
    /// This is used when an execution plan handles simple/nested properties but collections
    /// need to be handled via the legacy path.
    /// </summary>
    private void ApplyCollectionMappingOnly(object source, object destination,
        Type sourceType, Type destType, TypeMap typeMap)
    {
        var destProps = ReflectionCache.GetWritableProperties(destType);
        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);

        foreach (var destProp in destProps)
        {
            // Only process collection properties that were tracked during execution plan build
            if (!typeMap.CollectionProperties!.Contains(destProp.Name))
                continue;

            // Skip if ignored
            if (typeMap.IgnoredMembers?.Contains(destProp.Name) == true)
                continue;

            // Find source property
            if (!sourcePropsExact.TryGetValue(destProp.Name, out var sourceProp))
            {
                sourcePropsCI.TryGetValue(destProp.Name, out sourceProp);
            }

            if (sourceProp == null)
                continue;

            try
            {
                var sourceValue = ReflectionCache.GetValue(sourceProp, source);
                if (sourceValue != null)
                {
                    var convertedValue = ConvertValue(sourceValue, sourceProp.PropertyType, destProp.PropertyType);
                    ReflectionCache.SetValue(destProp, destination, convertedValue);
                }
                else
                {
                    // v12.1.0: Check for type converter first - it may want to handle null -> null mapping
                    var converterTypeMap = _registry.FindTypeMap(sourceProp.PropertyType, destProp.PropertyType);
                    if (converterTypeMap != null && (converterTypeMap.Converter != null || converterTypeMap.ConverterType != null))
                    {
                        // Let the converter handle null - it decides what to return
                        var convertedValue = ExecuteConverter(null!, sourceProp.PropertyType, destProp.PropertyType, converterTypeMap);
                        ReflectionCache.SetValue(destProp, destination, convertedValue);
                    }
                    else
                    {
                        // Handle null -> empty collection
                        var destPropAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);
                        if (destPropAnalysis.IsDictionary)
                        {
                            var emptyDict = CreateEmptyDictionary(destProp.PropertyType,
                                destPropAnalysis.DictionaryKeyType!, destPropAnalysis.DictionaryValueType!);
                            ReflectionCache.SetValue(destProp, destination, emptyDict);
                        }
                        else if (destPropAnalysis.IsCollection)
                        {
                            var emptyCollection = CreateEmptyCollection(destProp.PropertyType,
                                destPropAnalysis.CollectionElementType!);
                            ReflectionCache.SetValue(destProp, destination, emptyCollection);
                        }
                    }
                }
            }
            catch
            {
                // Skip properties that can't be mapped
            }
        }
    }

    /// <summary>
    /// Applies member maps for value types, returning the modified struct.
    /// </summary>
    private object ApplyMemberMapsForValueType(object source, object destination, TypeMap typeMap)
    {
        var destType = destination.GetType();
        var destPropsDict = ReflectionCache.GetWritablePropertiesDict(destType);

        foreach (var memberMap in typeMap.MemberMaps)
        {
            if (memberMap.Ignored)
                continue;

            // v10.0.0: Skip destination-dependent mappings here - they're applied later
            // in ApplyDestinationDependentMemberMapsForValueType after convention mapping
            if (memberMap.HasDestinationParameter)
                continue;

            destination = ApplySingleMemberMapForValueType(source, destination, memberMap, destPropsDict, typeMap);
        }

        return destination;
    }

    /// <summary>
    /// v10.0.0: Applies destination-dependent member mappings for value types.
    /// Called AFTER convention mapping so the destination is fully populated.
    /// </summary>
    private object ApplyDestinationDependentMemberMapsForValueType(object source, object destination, TypeMap typeMap)
    {
        var destType = destination.GetType();
        var destPropsDict = ReflectionCache.GetWritablePropertiesDict(destType);

        foreach (var memberMap in typeMap.MemberMaps)
        {
            if (memberMap.Ignored)
                continue;

            // Only apply destination-dependent mappings
            if (!memberMap.HasDestinationParameter)
                continue;

            destination = ApplyDestinationDependentMemberMapForValueType(source, destination, memberMap, destPropsDict, typeMap);
        }

        return destination;
    }

    /// <summary>
    /// Applies a single regular member map for value types.
    /// </summary>
    private object ApplySingleMemberMapForValueType(object source, object destination, MemberMap memberMap,
        Dictionary<string, System.Reflection.PropertyInfo> destPropsDict, TypeMap typeMap)
    {
        // v8.0.0: PreCondition evaluated BEFORE value resolution
        if (memberMap.PreCondition != null && !memberMap.PreCondition(source))
            return destination;

        if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destProp))
            return destination;

        object? value = null;

        // v12.0.0: IValueResolver takes priority
        if (memberMap.HasValueResolver)
        {
            try
            {
                var currentDestValue = ReflectionCache.GetValue(destProp, destination);
                value = InvokeValueResolver(memberMap, source, destination, currentDestValue);
            }
            catch
            {
                return destination;
            }
        }
        else if (memberMap.CompiledResolver != null)
        {
            try
            {
                value = memberMap.CompiledResolver(source);
            }
            catch
            {
                return destination;
            }
        }
        else if (memberMap.SourceValueResolver != null)
        {
            try
            {
                value = memberMap.SourceValueResolver.DynamicInvoke(source);
            }
            catch
            {
                return destination;
            }
        }

        // v8.0.0: Apply NullSubstitute if value is null
        if (value == null && memberMap.HasNullSubstitute)
        {
            value = memberMap.NullSubstitute;
        }

        // v8.0.0: Apply transforms (with destination property type for null handling)
        value = ApplyTransforms(value, typeMap, destProp.PropertyType);

        // v8.0.0: Condition evaluated AFTER value resolution (and transforms)
        if (memberMap.ConditionWithContext != null)
        {
            if (!memberMap.ConditionWithContext(source, destination, value, _context))
                return destination;
        }
        else if (memberMap.Condition != null)
        {
            if (!memberMap.Condition(source, destination, value))
                return destination;
        }

        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, destProp.PropertyType);
            destination = ReflectionCache.SetValueOnValueType(destProp, destination, convertedValue);
        }

        return destination;
    }

    /// <summary>
    /// v10.0.0: Applies a destination-dependent member map for value types.
    /// </summary>
    private object ApplyDestinationDependentMemberMapForValueType(object source, object destination, MemberMap memberMap,
        Dictionary<string, System.Reflection.PropertyInfo> destPropsDict, TypeMap typeMap)
    {
        // v8.0.0: PreCondition evaluated BEFORE value resolution
        if (memberMap.PreCondition != null && !memberMap.PreCondition(source))
            return destination;

        if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destProp))
            return destination;

        object? value = null;

        // v10.0.0: Use DestinationResolver which passes the actual destination
        if (memberMap.DestinationResolver != null)
        {
            try
            {
                value = memberMap.DestinationResolver(source, destination);
            }
            catch
            {
                return destination;
            }
        }
        else if (memberMap.CompiledResolver != null)
        {
            try
            {
                value = memberMap.CompiledResolver(source);
            }
            catch
            {
                return destination;
            }
        }

        // v8.0.0: Apply NullSubstitute if value is null
        if (value == null && memberMap.HasNullSubstitute)
        {
            value = memberMap.NullSubstitute;
        }

        // v8.0.0: Apply transforms (with destination property type for null handling)
        value = ApplyTransforms(value, typeMap, destProp.PropertyType);

        // v8.0.0: Condition evaluated AFTER value resolution (and transforms)
        if (memberMap.ConditionWithContext != null)
        {
            if (!memberMap.ConditionWithContext(source, destination, value, _context))
                return destination;
        }
        else if (memberMap.Condition != null)
        {
            if (!memberMap.Condition(source, destination, value))
                return destination;
        }

        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, destProp.PropertyType);
            destination = ReflectionCache.SetValueOnValueType(destProp, destination, convertedValue);
        }

        return destination;
    }

    /// <summary>
    /// Applies convention mapping for value types, returning the modified struct.
    /// </summary>
    private object ApplyConventionMappingForValueType(object source, object destination,
        Type sourceType, Type destType, TypeMap? typeMap)
    {
        var destProps = ReflectionCache.GetWritableProperties(destType);
        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);

        // Use pre-computed member sets from TypeMap (no allocations)
        var configuredMembers = typeMap?.ConfiguredMembers;
        var ignoredMembers = typeMap?.IgnoredMembers;

        foreach (var destProp in destProps)
        {
            if (configuredMembers != null && configuredMembers.Contains(destProp.Name))
                continue;
            if (ignoredMembers != null && ignoredMembers.Contains(destProp.Name))
                continue;

            // Try exact match first (case-sensitive), then fall back to case-insensitive
            if (!sourcePropsExact.TryGetValue(destProp.Name, out var sourceProp))
            {
                sourcePropsCI.TryGetValue(destProp.Name, out sourceProp);
            }

            if (sourceProp != null)
            {
                try
                {
                    var sourceValue = ReflectionCache.GetValue(sourceProp, source);

                    // v8.0.0: Apply transforms (with destination property type for null handling)
                    var transformedValue = ApplyTransforms(sourceValue, typeMap, destProp.PropertyType);

                    if (transformedValue != null)
                    {
                        var convertedValue = ConvertValue(transformedValue, sourceProp.PropertyType, destProp.PropertyType);
                        destination = ReflectionCache.SetValueOnValueType(destProp, destination, convertedValue);
                    }
                    else
                    {
                        // Use cached type analysis for null value handling
                        var destPropAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);
                        if (destPropAnalysis.IsDictionary)
                        {
                            var emptyDict = CreateEmptyDictionary(destProp.PropertyType, destPropAnalysis.DictionaryKeyType!, destPropAnalysis.DictionaryValueType!);
                            destination = ReflectionCache.SetValueOnValueType(destProp, destination, emptyDict);
                        }
                        else if (destPropAnalysis.IsCollection)
                        {
                            var emptyCollection = CreateEmptyCollection(destProp.PropertyType, destPropAnalysis.CollectionElementType!);
                            destination = ReflectionCache.SetValueOnValueType(destProp, destination, emptyCollection);
                        }
                    }
                }
                catch
                {
                    // Skip properties that can't be mapped
                }
            }
        }

        return destination;
    }

    private object? ConvertValue(object value, Type sourceType, Type destType)
    {
        if (value == null)
            return null;

        // Direct assignment if compatible
        if (destType.IsAssignableFrom(sourceType))
            return value;

        // Use cached type analysis
        var sourceAnalysis = ReflectionCache.GetTypeAnalysis(sourceType);
        var destAnalysis = ReflectionCache.GetTypeAnalysis(destType);

        // Handle nullable source - unwrap the value
        if (sourceAnalysis.UnderlyingNullableType != null)
        {
            // The value is already unboxed, so just recurse with the underlying type
            return ConvertValue(value, sourceAnalysis.UnderlyingNullableType, destType);
        }

        // Handle nullable destination
        if (destAnalysis.UnderlyingNullableType != null)
        {
            // Map to the underlying type; boxing will handle Nullable<T>
            return ConvertValue(value, sourceType, destAnalysis.UnderlyingNullableType);
        }

        // Handle struct to struct mapping (value types that are not simple types)
        if (sourceType.IsValueType && destType.IsValueType && !sourceAnalysis.IsSimple && !destAnalysis.IsSimple)
        {
            return MapInternal(value, sourceType, destType);
        }

        // Handle enum conversions
        if (destType.IsEnum && sourceType == typeof(int))
        {
            return Enum.ToObject(destType, value);
        }

        if (sourceType.IsEnum && destType == typeof(int))
        {
            return Convert.ToInt32(value);
        }

        // String to enum conversion
        if (destType.IsEnum && sourceType == typeof(string))
        {
            var stringValue = (string)value;
            if (Enum.TryParse(destType, stringValue, ignoreCase: true, out var enumResult))
                return enumResult;
            return Enum.ToObject(destType, 0); // Default value if parsing fails
        }

        // Enum to string conversion
        if (sourceType.IsEnum && destType == typeof(string))
        {
            return value.ToString();
        }

        // Handle DateTime <-> DateOnly conversions
        if (sourceType == typeof(DateTime) && destType == typeof(DateOnly))
        {
            return DateOnly.FromDateTime((DateTime)value);
        }

        if (sourceType == typeof(DateOnly) && destType == typeof(DateTime))
        {
            return ((DateOnly)value).ToDateTime(TimeOnly.MinValue);
        }

        // Handle dictionary mappings (before collections, since dictionaries implement IEnumerable)
        if (destAnalysis.IsDictionary && sourceAnalysis.IsDictionary)
        {
            return MapDictionary(value, sourceAnalysis.DictionaryKeyType!, sourceAnalysis.DictionaryValueType!,
                destAnalysis.DictionaryKeyType!, destAnalysis.DictionaryValueType!);
        }

        // v12.1.0: Check for type converter before collection handling
        // This allows converters like string -> IList<T> to work
        {
            var typeMap = _registry.FindTypeMap(sourceType, destType);
            if (typeMap != null && (typeMap.Converter != null || typeMap.ConverterType != null))
            {
                return ExecuteConverter(value, sourceType, destType, typeMap);
            }
        }

        // Handle collection mappings
        if (destAnalysis.IsEnumerable && sourceAnalysis.IsEnumerable)
        {
            return MapCollection(value, sourceAnalysis.EnumerableElementType!, destAnalysis.EnumerableElementType!, destType);
        }

        // Handle nested object mappings
        if (!destAnalysis.IsSimple && !sourceAnalysis.IsSimple)
        {
            // Check if we have a registered mapping
            var typeMap = _registry.FindTypeMap(sourceType, destType);
            if (typeMap != null || ShouldAttemptConventionMapping(sourceType, destType))
            {
                return MapInternal(value, sourceType, destType);
            }
        }

        // Handle simple conversions
        if (destType == typeof(string))
            return value.ToString();

        try
        {
            return Convert.ChangeType(value, destType);
        }
        catch
        {
            return value;
        }
    }

    private object? ConvertValueForProperty(object value, Type destType)
    {
        if (value == null)
            return null;

        var sourceType = value.GetType();
        return ConvertValue(value, sourceType, destType);
    }

    private bool ShouldAttemptConventionMapping(Type sourceType, Type destType)
    {
        // Don't try to map collections here - they should be handled by MapCollection
        if (typeof(IEnumerable).IsAssignableFrom(sourceType) && sourceType != typeof(string))
            return false;

        // Only attempt convention mapping for class types
        return sourceType.IsClass && destType.IsClass;
    }

    private object? ExecuteConverter(object source, Type sourceType, Type destType, TypeMap typeMap, object? existingDestination = null)
    {
        var converter = typeMap.Converter;
        if (converter == null && typeMap.ConverterType != null)
        {
            var converterType = typeMap.ConverterType;

            // Handle open generic converters
            if (converterType.IsGenericTypeDefinition)
            {
                Type[] genericArgs;

                // Get generic arguments from the actual source/dest types
                if (sourceType.IsGenericType && destType.IsGenericType)
                {
                    var sourceArgs = sourceType.GetGenericArguments();
                    var destArgs = destType.GetGenericArguments();

                    // Combine args based on converter type parameter count
                    var converterGenericParams = converterType.GetGenericArguments();
                    if (converterGenericParams.Length == 2)
                    {
                        genericArgs = new[] { sourceArgs[0], destArgs[0] };
                    }
                    else
                    {
                        genericArgs = sourceArgs.Concat(destArgs).Take(converterGenericParams.Length).ToArray();
                    }
                }
                else
                {
                    genericArgs = new[] { sourceType, destType };
                }

                converterType = converterType.MakeGenericType(genericArgs);
            }

            converter = ReflectionCache.CreateInstance(converterType);
        }

        var convertMethod = converter!.GetType().GetMethod("Convert");
        return convertMethod!.Invoke(converter, new[] { source, existingDestination, _context });
    }

    private static bool IsEnumerableType(Type type, out Type? elementType)
    {
        elementType = null;

        if (type == typeof(string))
            return false;

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }

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
                genericDef == typeof(ObservableCollection<>) ||
                genericDef == typeof(LinkedList<>) ||
                genericDef == typeof(Queue<>) ||
                genericDef == typeof(Stack<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        // Check for IEnumerable<T> interface
        var enumInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface != null)
        {
            elementType = enumInterface.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private static bool IsSimpleType(Type type)
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

    private static bool IsNullableType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    /// <summary>
    /// Checks if the type is a collection type (excluding string) and extracts the element type.
    /// </summary>
    private static bool IsCollectionType(Type type, out Type? elementType)
    {
        elementType = null;

        if (type == typeof(string))
            return false;

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }

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
                genericDef == typeof(ObservableCollection<>) ||
                genericDef == typeof(LinkedList<>) ||
                genericDef == typeof(Queue<>) ||
                genericDef == typeof(Stack<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        // Check for IEnumerable<T> interface (custom collections)
        var enumInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface != null)
        {
            elementType = enumInterface.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates an empty collection of the specified type.
    /// </summary>
    private static object CreateEmptyCollection(Type collectionType, Type elementType)
    {
        if (collectionType.IsArray)
        {
            return Array.CreateInstance(elementType, 0);
        }

        // For interfaces or List<T>, create a List<T>
        var listType = typeof(List<>).MakeGenericType(elementType);
        return ReflectionCache.CreateInstance(listType);
    }

    /// <summary>
    /// Checks if the type is a dictionary type and extracts key/value types.
    /// </summary>
    private static bool IsDictionaryType(Type type, out Type? keyType, out Type? valueType)
    {
        keyType = null;
        valueType = null;

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>) ||
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                var args = type.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }
        }

        // Check for IDictionary<,> interface
        var dictInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictInterface != null)
        {
            var args = dictInterface.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates an empty dictionary of the specified type.
    /// </summary>
    private static object CreateEmptyDictionary(Type dictType, Type keyType, Type valueType)
    {
        var concreteType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        return ReflectionCache.CreateInstance(concreteType);
    }

    /// <summary>
    /// Maps a dictionary with potentially complex values.
    /// v5.0.0: Pre-allocates capacity to reduce reallocations.
    /// </summary>
    private object MapDictionary(object source, Type srcKeyType, Type srcValueType, Type destKeyType, Type destValueType)
    {
        var sourceDict = (IDictionary)source;
        var destDictType = typeof(Dictionary<,>).MakeGenericType(destKeyType, destValueType);

        // v5.0.0: Pre-allocate dictionary with source capacity to avoid rehashing
        var destDict = (IDictionary)Activator.CreateInstance(destDictType, sourceDict.Count)!;

        // Use cached type analysis
        var srcValueAnalysis = ReflectionCache.GetTypeAnalysis(srcValueType);
        var destValueAnalysis = ReflectionCache.GetTypeAnalysis(destValueType);
        var isSimpleValues = srcValueAnalysis.IsSimple && destValueAnalysis.IsSimple;

        foreach (DictionaryEntry entry in sourceDict)
        {
            var mappedKey = ConvertValue(entry.Key, srcKeyType, destKeyType);
            object? mappedValue;

            if (entry.Value == null)
            {
                mappedValue = null;
            }
            else if (isSimpleValues)
            {
                mappedValue = ConvertValue(entry.Value, srcValueType, destValueType);
            }
            else
            {
                // Complex value - use MapInternal
                mappedValue = MapInternal(entry.Value, srcValueType, destValueType);
            }

            destDict[mappedKey!] = mappedValue;
        }

        return destDict;
    }

    private void MapToExisting(object source, object destination, Type sourceType, Type destType)
    {
        var typeMap = _registry.FindTypeMap(sourceType, destType);

        // Lambda converters don't support existing destination - create new
        if (typeMap?.LambdaConverter != null)
        {
            var result = typeMap.LambdaConverter.DynamicInvoke(source);
            // Copy result properties to destination
            if (result != null)
            {
                var props = ReflectionCache.GetWritableProperties(destType);
                var sourceProps = ReflectionCache.GetReadablePropertiesDict(destType);
                foreach (var prop in props)
                {
                    if (sourceProps.TryGetValue(prop.Name, out var srcProp))
                    {
                        var value = ReflectionCache.GetValue(srcProp, result);
                        ReflectionCache.SetValue(prop, destination, value);
                    }
                }
            }
            return;
        }

        // If there's a converter, use it with the existing destination
        if (typeMap?.Converter != null || typeMap?.ConverterType != null)
        {
            ExecuteConverter(source, sourceType, destType, typeMap, destination);
            return;
        }

        // v8.0.0: Execute BeforeMap if configured
        ExecuteBeforeMap(source, destination, typeMap);

        // v8.0.0: Apply IncludeBase configuration first (if any)
        ApplyIncludeBaseConfiguration(source, destination, typeMap);

        if (typeMap != null)
        {
            ApplyMemberMaps(source, destination, typeMap);
        }
        ApplyConventionMapping(source, destination, sourceType, destType, typeMap);

        // v10.0.0: Apply destination-dependent mappings AFTER convention mapping
        if (typeMap != null)
        {
            ApplyDestinationDependentMemberMaps(source, destination, typeMap);
        }

        // v8.0.0: Apply ForPath mappings (after member maps, before AfterMap)
        if (typeMap != null)
        {
            ApplyPathMaps(source, destination, typeMap);
        }

        // v8.0.0: Execute AfterMap if configured
        ExecuteAfterMap(source, destination, typeMap);
    }

    #region v8.0.0: IncludeBase Configuration

    /// <summary>
    /// Applies member configuration from base type mappings when IncludeBase is used.
    /// </summary>
    private void ApplyIncludeBaseConfiguration(object source, object destination, TypeMap? typeMap)
    {
        if (typeMap?.IncludedBaseType == null)
            return;

        var (baseSourceType, baseDestType) = typeMap.IncludedBaseType.Value;
        var baseTypeMap = _registry.FindTypeMap(baseSourceType, baseDestType);

        if (baseTypeMap == null)
            return;

        // Apply base type's member maps (these have lower priority than derived type's)
        ApplyBaseMemberMaps(source, destination, baseTypeMap, typeMap);

        // Recursively apply any further IncludeBase configuration
        ApplyIncludeBaseConfiguration(source, destination, baseTypeMap);
    }

    /// <summary>
    /// Applies member maps from base TypeMap that aren't overridden in derived TypeMap.
    /// </summary>
    private void ApplyBaseMemberMaps(object source, object destination, TypeMap baseTypeMap, TypeMap derivedTypeMap)
    {
        var destType = destination.GetType();
        var destPropsDict = ReflectionCache.GetWritablePropertiesDict(destType);

        // Get members already configured in derived type map
        var derivedConfigured = derivedTypeMap.ConfiguredMembers ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // v10.0.0: Collect destination-dependent mappings to apply after regular mappings
        List<MemberMap>? deferredMappings = null;

        foreach (var memberMap in baseTypeMap.MemberMaps)
        {
            // Skip if derived type already has configuration for this member
            if (derivedConfigured.Contains(memberMap.DestinationMemberName))
                continue;

            if (memberMap.Ignored)
                continue;

            // v10.0.0: Defer destination-dependent mappings until after regular mappings
            if (memberMap.HasDestinationParameter)
            {
                deferredMappings ??= new List<MemberMap>();
                deferredMappings.Add(memberMap);
                continue;
            }

            ApplyBaseSingleMemberMap(source, destination, memberMap, destPropsDict);
        }

        // v10.0.0: Now apply destination-dependent mappings (destination is populated)
        if (deferredMappings != null)
        {
            foreach (var memberMap in deferredMappings)
            {
                ApplyBaseDestinationDependentMemberMap(source, destination, memberMap, destPropsDict);
            }
        }
    }

    /// <summary>
    /// Applies a single regular member map from base TypeMap.
    /// </summary>
    private void ApplyBaseSingleMemberMap(object source, object destination, MemberMap memberMap,
        Dictionary<string, System.Reflection.PropertyInfo> destPropsDict)
    {
        // v8.0.0: PreCondition evaluated BEFORE value resolution
        if (memberMap.PreCondition != null && !memberMap.PreCondition(source))
            return;

        if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destProp))
            return;

        object? value = null;

        // v12.0.0: IValueResolver takes priority
        if (memberMap.HasValueResolver)
        {
            try
            {
                var currentDestValue = ReflectionCache.GetValue(destProp, destination);
                value = InvokeValueResolver(memberMap, source, destination, currentDestValue);
            }
            catch
            {
                return;
            }
        }
        else if (memberMap.CompiledResolver != null)
        {
            try
            {
                value = memberMap.CompiledResolver(source);
            }
            catch
            {
                return;
            }
        }
        else if (memberMap.SourceValueResolver != null)
        {
            try
            {
                value = memberMap.SourceValueResolver.DynamicInvoke(source);
            }
            catch
            {
                return;
            }
        }

        // v8.0.0: Apply NullSubstitute if value is null
        if (value == null && memberMap.HasNullSubstitute)
        {
            value = memberMap.NullSubstitute;
        }

        // v8.0.0: Condition evaluated AFTER value resolution
        if (memberMap.ConditionWithContext != null)
        {
            if (!memberMap.ConditionWithContext(source, destination, value, _context))
                return;
        }
        else if (memberMap.Condition != null)
        {
            if (!memberMap.Condition(source, destination, value))
                return;
        }

        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, destProp.PropertyType);
            ReflectionCache.SetValue(destProp, destination, convertedValue);
        }
    }

    /// <summary>
    /// v10.0.0: Applies a destination-dependent member map from base TypeMap.
    /// </summary>
    private void ApplyBaseDestinationDependentMemberMap(object source, object destination, MemberMap memberMap,
        Dictionary<string, System.Reflection.PropertyInfo> destPropsDict)
    {
        // v8.0.0: PreCondition evaluated BEFORE value resolution
        if (memberMap.PreCondition != null && !memberMap.PreCondition(source))
            return;

        if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destProp))
            return;

        object? value = null;

        // v10.0.0: Use DestinationResolver which passes the actual destination
        if (memberMap.DestinationResolver != null)
        {
            try
            {
                value = memberMap.DestinationResolver(source, destination);
            }
            catch
            {
                return;
            }
        }
        else if (memberMap.CompiledResolver != null)
        {
            try
            {
                value = memberMap.CompiledResolver(source);
            }
            catch
            {
                return;
            }
        }

        // v8.0.0: Apply NullSubstitute if value is null
        if (value == null && memberMap.HasNullSubstitute)
        {
            value = memberMap.NullSubstitute;
        }

        // v8.0.0: Condition evaluated AFTER value resolution
        if (memberMap.ConditionWithContext != null)
        {
            if (!memberMap.ConditionWithContext(source, destination, value, _context))
                return;
        }
        else if (memberMap.Condition != null)
        {
            if (!memberMap.Condition(source, destination, value))
                return;
        }

        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, destProp.PropertyType);
            ReflectionCache.SetValue(destProp, destination, convertedValue);
        }
    }

    #endregion

    #region v8.0.0: ForPath Mappings

    /// <summary>
    /// Applies ForPath mappings for deeply nested property paths.
    /// Creates intermediate objects as needed.
    /// </summary>
    private void ApplyPathMaps(object source, object destination, TypeMap typeMap)
    {
        if (typeMap.PathMaps.Count == 0)
            return;

        foreach (var pathMap in typeMap.PathMaps)
        {
            if (pathMap.Ignored)
                continue;

            // Check condition if present
            if (pathMap.Condition != null && !pathMap.Condition(source))
                continue;

            // Get the value to set
            object? value = null;
            if (pathMap.SourceValueResolver != null)
            {
                try
                {
                    value = pathMap.SourceValueResolver.DynamicInvoke(source);
                }
                catch
                {
                    // If resolver fails, skip this path
                    continue;
                }
            }

            // Navigate and set the value at the path
            SetValueAtPath(destination, pathMap.PathSegments, value);
        }
    }

    /// <summary>
    /// Sets a value at a deeply nested property path, creating intermediate objects as needed.
    /// </summary>
    private void SetValueAtPath(object destination, List<string> pathSegments, object? value)
    {
        if (pathSegments.Count == 0)
            return;

        var current = destination;
        var currentType = destination.GetType();

        // Navigate to the parent of the leaf property, creating intermediates as needed
        for (int i = 0; i < pathSegments.Count - 1; i++)
        {
            var segmentName = pathSegments[i];
            var propInfo = currentType.GetProperty(segmentName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propInfo == null)
                return; // Property not found, skip

            var propValue = propInfo.GetValue(current);

            // If intermediate is null, create it
            if (propValue == null)
            {
                var intermediateCtor = propInfo.PropertyType.GetConstructor(Type.EmptyTypes);
                if (intermediateCtor == null)
                    return; // Can't create intermediate, skip

                propValue = intermediateCtor.Invoke(null);
                propInfo.SetValue(current, propValue);
            }

            current = propValue;
            currentType = propInfo.PropertyType;
        }

        // Set the leaf property
        var leafSegment = pathSegments[^1];
        var leafProp = currentType.GetProperty(leafSegment,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (leafProp == null || !leafProp.CanWrite)
            return;

        // Convert value if needed
        if (value != null)
        {
            var convertedValue = ConvertValueForProperty(value, leafProp.PropertyType);
            leafProp.SetValue(current, convertedValue);
        }
        else if (!leafProp.PropertyType.IsValueType || Nullable.GetUnderlyingType(leafProp.PropertyType) != null)
        {
            leafProp.SetValue(current, null);
        }
    }

    #endregion

    #region v8.0.0: Lifecycle Hooks

    /// <summary>
    /// Executes BeforeMap hook if configured.
    /// </summary>
    private void ExecuteBeforeMap(object source, object destination, TypeMap? typeMap)
    {
        if (typeMap == null || !typeMap.HasBeforeMap)
            return;

        try
        {
            if (typeMap.BeforeMapWithContext != null)
            {
                ((Action<object, object, ResolutionContext>)typeMap.BeforeMapWithContext)(source, destination, _context);
            }
            else if (typeMap.BeforeMap != null)
            {
                ((Action<object, object>)typeMap.BeforeMap)(source, destination);
            }
        }
        catch
        {
            // BeforeMap errors are propagated - don't swallow them
            throw;
        }
    }

    /// <summary>
    /// Executes AfterMap hook if configured.
    /// </summary>
    private void ExecuteAfterMap(object source, object destination, TypeMap? typeMap)
    {
        if (typeMap == null || !typeMap.HasAfterMap)
            return;

        try
        {
            if (typeMap.AfterMapWithContext != null)
            {
                ((Action<object, object, ResolutionContext>)typeMap.AfterMapWithContext)(source, destination, _context);
            }
            else if (typeMap.AfterMap != null)
            {
                ((Action<object, object>)typeMap.AfterMap)(source, destination);
            }
        }
        catch
        {
            // AfterMap errors are propagated - don't swallow them
            throw;
        }
    }

    #endregion

    #region v12.0.0: IValueResolver Support

    /// <summary>
    /// Invokes an IValueResolver to resolve a member value.
    /// </summary>
    private object? InvokeValueResolver(MemberMap memberMap, object source, object destination, object? currentDestValue)
    {
        if (!memberMap.HasValueResolver) return currentDestValue;

        // Get or create resolver instance
        var resolver = memberMap.ResolverInstance
            ?? _serviceLocator?.Invoke(memberMap.ResolverType!)
            ?? Activator.CreateInstance(memberMap.ResolverType!);

        if (resolver == null) return currentDestValue;

        // Use cached compiled delegate for performance
        var del = GetOrCreateResolverDelegate(memberMap.ResolverType!);
        return del(resolver, source, destination, currentDestValue, _context);
    }

    /// <summary>
    /// Gets or creates a compiled delegate for invoking an IValueResolver.
    /// The delegate avoids reflection overhead after the first call.
    /// </summary>
    private static Func<object, object, object, object?, ResolutionContext, object?>
        GetOrCreateResolverDelegate(Type resolverType)
    {
        return _resolverDelegateCache.GetOrAdd(resolverType, type =>
        {
            var resolverInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValueResolver<,,>));

            if (resolverInterface == null)
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' does not implement IValueResolver<,,>.");
            }

            var typeArgs = resolverInterface.GetGenericArguments();

            var resolverParam = Expression.Parameter(typeof(object), "resolver");
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var destParam = Expression.Parameter(typeof(object), "dest");
            var memberParam = Expression.Parameter(typeof(object), "member");
            var contextParam = Expression.Parameter(typeof(ResolutionContext), "context");

            // Handle null member value for value types
            Expression convertedMember;
            if (typeArgs[2].IsValueType)
            {
                // For value types: member == null ? default(T) : (T)member
                convertedMember = Expression.Condition(
                    Expression.Equal(memberParam, Expression.Constant(null)),
                    Expression.Default(typeArgs[2]),
                    Expression.Convert(memberParam, typeArgs[2]));
            }
            else
            {
                // For reference types: just cast
                convertedMember = Expression.Convert(memberParam, typeArgs[2]);
            }

            var call = Expression.Call(
                Expression.Convert(resolverParam, type),
                "Resolve",
                Type.EmptyTypes,
                Expression.Convert(sourceParam, typeArgs[0]),
                Expression.Convert(destParam, typeArgs[1]),
                convertedMember,
                contextParam);

            return Expression.Lambda<Func<object, object, object, object?, ResolutionContext, object?>>(
                Expression.Convert(call, typeof(object)),
                resolverParam, sourceParam, destParam, memberParam, contextParam).Compile();
        });
    }

    #endregion
}
