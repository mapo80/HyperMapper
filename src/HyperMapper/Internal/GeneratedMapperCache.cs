using System.Runtime.CompilerServices;

namespace HyperMapper.Internal;

/// <summary>
/// v8.0.0: Instance-aware generic cache for generated mappers.
/// Uses a combination of static generic cache and instance verification.
/// After first access with the same dictionary reference, retrieval is ~1ns.
/// </summary>
internal static class GeneratedMapperCache<TSource, TDest>
{
    // Static field - initialized once per type pair per dictionary instance
    private static Func<TSource?, TDest?>? _cachedMapper;
    private static Dictionary<(Type, Type), Delegate>? _cachedPlansRef;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the cached mapper delegate, initializing from the plans dictionary if needed.
    /// Thread-safe with minimal overhead after initialization.
    /// Verifies the dictionary reference matches to support multiple mapper instances.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<TSource?, TDest?>? GetMapper(Dictionary<(Type, Type), Delegate> plans)
    {
        // Fast path: already initialized with the same dictionary instance
        if (ReferenceEquals(_cachedPlansRef, plans))
            return _cachedMapper;

        // Slow path: initialization or different dictionary (thread-safe)
        lock (_lock)
        {
            // Double-check after acquiring lock
            if (ReferenceEquals(_cachedPlansRef, plans))
                return _cachedMapper;

            // Initialize for this dictionary instance
            if (plans.TryGetValue((typeof(TSource), typeof(TDest)), out var plan))
            {
                _cachedMapper = (Func<TSource?, TDest?>)plan;
            }
            else
            {
                _cachedMapper = null;
            }
            _cachedPlansRef = plans;
        }

        return _cachedMapper;
    }

    /// <summary>
    /// Resets the cache. Used for testing or when reconfiguring mappers.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _cachedMapper = null;
            _cachedPlansRef = null;
        }
    }
}
