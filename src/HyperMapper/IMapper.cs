namespace HyperMapper;

/// <summary>
/// Main mapper interface - 100% compatible with AutoMapper.IMapper
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Execute a mapping from the source object to a new destination object.
    /// </summary>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Execute a mapping from the source object to the existing destination object.
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>
    /// Execute a mapping from the source object to a new destination object with explicit types.
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    /// Execute a mapping from the source object to a new destination object.
    /// </summary>
    object Map(object source, Type sourceType, Type destinationType);

    /// <summary>
    /// Execute a mapping from the source object to an existing destination object.
    /// </summary>
    object Map(object source, object destination, Type sourceType, Type destinationType);
}
