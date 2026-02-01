using HyperMapper.Configuration;
using HyperMapper.Internal;

namespace HyperMapper;

/// <summary>
/// Base class for mapping profiles - compatible with AutoMapper.Profile
/// </summary>
public abstract class Profile
{
    internal List<TypeMap> TypeMaps { get; } = new();

    /// <summary>
    /// Create a mapping configuration from TSource to TDestination.
    /// </summary>
    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var typeMap = new TypeMap(typeof(TSource), typeof(TDestination));
        TypeMaps.Add(typeMap);
        return new MappingExpression<TSource, TDestination>(typeMap, this);
    }

    /// <summary>
    /// Create a mapping configuration for open generic types.
    /// </summary>
    protected IMappingExpressionBase CreateMap(Type sourceType, Type destinationType)
    {
        var typeMap = new TypeMap(sourceType, destinationType, isOpenGeneric: true);
        TypeMaps.Add(typeMap);
        return new MappingExpressionBase(typeMap);
    }

    internal void AddTypeMap(TypeMap typeMap)
    {
        TypeMaps.Add(typeMap);
    }
}
