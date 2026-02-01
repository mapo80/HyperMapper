using System.Linq.Expressions;

namespace HyperMapper.Configuration;

/// <summary>
/// Base mapping expression interface for non-generic scenarios.
/// </summary>
public interface IMappingExpressionBase
{
    IMappingExpressionBase ConvertUsing(Type converterType);
}

/// <summary>
/// Mapping expression interface for configuring type mappings - compatible with AutoMapper.IMappingExpression
/// </summary>
public interface IMappingExpression<TSource, TDestination> : IMappingExpressionBase
{
    /// <summary>
    /// Configure a member mapping.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

    /// <summary>
    /// Create a reverse mapping from TDestination to TSource.
    /// </summary>
    IMappingExpression<TDestination, TSource> ReverseMap();

    /// <summary>
    /// Use a custom type converter.
    /// </summary>
    new IMappingExpression<TSource, TDestination> ConvertUsing(Type converterType);

    /// <summary>
    /// Use a custom type converter instance.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter);

    /// <summary>
    /// Use a custom type converter by type.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>()
        where TConverter : ITypeConverter<TSource, TDestination>, new();

    /// <summary>
    /// Use a lambda expression as converter.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter);

    // ===== v8.0.0: New AutoMapper-compatible features =====

    /// <summary>
    /// Add a value transformation for all members of the specified type.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> AddTransform<TValue>(Expression<Func<TValue, TValue>> transformer);

    /// <summary>
    /// Execute an action before mapping.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction);

    /// <summary>
    /// Execute an action before mapping with ResolutionContext.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> beforeFunction);

    /// <summary>
    /// Execute an action after mapping.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction);

    /// <summary>
    /// Execute an action after mapping with ResolutionContext.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> afterFunction);

    /// <summary>
    /// Configure a deeply nested property path.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IPathConfigurationExpression<TSource, TDestination, TMember>> pathOptions);

    /// <summary>
    /// Set the maximum depth for this type mapping.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> MaxDepth(int depth);

    /// <summary>
    /// Preserve object references (useful for circular references).
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> PreserveReferences();

    /// <summary>
    /// Include additional source members to flatten into the destination.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> IncludeMembers(params Expression<Func<TSource, object?>>[] memberExpressions);

    /// <summary>
    /// Include a derived type mapping.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
        where TDerivedSource : TSource
        where TDerivedDestination : TDestination;

    /// <summary>
    /// Include base type configuration.
    /// AutoMapper API compatible.
    /// Note: TSource must be assignable to TBaseSource, TDestination must be assignable to TBaseDestination.
    /// </summary>
    IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>();

    /// <summary>
    /// Control which members are validated.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ValidateMemberList(MemberList memberList);

    /// <summary>
    /// Use a custom constructor function.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> constructor);

    /// <summary>
    /// Use a custom constructor function with ResolutionContext.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, ResolutionContext, TDestination> constructor);

    /// <summary>
    /// Configure a constructor parameter mapping.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForCtorParam(
        string ctorParamName,
        Action<ICtorParamConfigurationExpression<TSource>> config);

    /// <summary>
    /// Apply configuration to all destination members.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);

    /// <summary>
    /// Apply configuration to all destination members not explicitly configured.
    /// AutoMapper API compatible.
    /// </summary>
    IMappingExpression<TSource, TDestination> ForAllOtherMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);
}
