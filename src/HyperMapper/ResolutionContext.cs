namespace HyperMapper;

/// <summary>
/// Context passed to type converters - compatible with AutoMapper.ResolutionContext
/// </summary>
public class ResolutionContext
{
    /// <summary>
    /// Gets the mapper instance for nested mappings.
    /// </summary>
    public IMapper Mapper { get; }

    // v8.0.0: Depth tracking per type-pair for MaxDepth feature
    private readonly Dictionary<(Type, Type), int> _typeMappingDepth = new();

    // v8.0.0: Object reference cache for PreserveReferences feature
    private readonly Dictionary<object, object> _mappedObjects = new(ReferenceEqualityComparer.Instance);

    internal ResolutionContext(IMapper mapper)
    {
        Mapper = mapper;
    }

    #region v8.0.0: Depth Tracking

    /// <summary>
    /// Checks if mapping should continue based on MaxDepth setting.
    /// Returns true if we should map, false if we've exceeded depth.
    /// </summary>
    internal bool ShouldMapNested(Type sourceType, Type destType, int? maxDepth)
    {
        if (!maxDepth.HasValue)
            return true; // No limit configured

        var key = (sourceType, destType);
        _typeMappingDepth.TryGetValue(key, out var currentDepth);

        return currentDepth < maxDepth.Value;
    }

    /// <summary>
    /// Increments the depth counter for a type pair before mapping.
    /// </summary>
    internal void IncrementDepth(Type sourceType, Type destType)
    {
        var key = (sourceType, destType);
        _typeMappingDepth.TryGetValue(key, out var currentDepth);
        _typeMappingDepth[key] = currentDepth + 1;
    }

    /// <summary>
    /// Decrements the depth counter for a type pair after mapping.
    /// </summary>
    internal void DecrementDepth(Type sourceType, Type destType)
    {
        var key = (sourceType, destType);
        if (_typeMappingDepth.TryGetValue(key, out var currentDepth) && currentDepth > 0)
        {
            _typeMappingDepth[key] = currentDepth - 1;
        }
    }

    /// <summary>
    /// Gets the current depth for a type pair.
    /// </summary>
    internal int GetCurrentDepth(Type sourceType, Type destType)
    {
        var key = (sourceType, destType);
        _typeMappingDepth.TryGetValue(key, out var currentDepth);
        return currentDepth;
    }

    #endregion

    #region v8.0.0: Reference Preservation

    /// <summary>
    /// Checks if we've already mapped this source object.
    /// </summary>
    internal bool TryGetMappedObject(object source, Type destType, out object? mapped)
    {
        mapped = null;
        if (_mappedObjects.TryGetValue(source, out var existing))
        {
            // Verify the destination type matches
            if (destType.IsAssignableFrom(existing.GetType()))
            {
                mapped = existing;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Tracks a mapped object for reference preservation.
    /// </summary>
    internal void TrackMappedObject(object source, object destination)
    {
        _mappedObjects[source] = destination;
    }

    #endregion
}

/// <summary>
/// Compares objects by reference equality only.
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public static readonly ReferenceEqualityComparer Instance = new();

    private ReferenceEqualityComparer() { }

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
