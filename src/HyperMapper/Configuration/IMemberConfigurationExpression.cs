using System.Linq.Expressions;

namespace HyperMapper.Configuration;

/// <summary>
/// Member configuration expression interface - compatible with AutoMapper.IMemberConfigurationExpression
/// </summary>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    /// Map from a source member.
    /// </summary>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);

    /// <summary>
    /// Map from a custom function.
    /// </summary>
    void MapFrom<TResult>(Func<TSource, TDestination, TResult> mappingFunction);


    /// <summary>
    /// Ignore this member during mapping.
    /// </summary>
    void Ignore();

    /// <summary>
    /// Apply a pre-condition for this member mapping.
    /// The condition is evaluated BEFORE the value is resolved.
    /// </summary>
    void PreCondition(Func<TSource, bool> condition);

    /// <summary>
    /// Apply a post-condition for this member mapping.
    /// The condition is evaluated AFTER the value is resolved, with access to source, destination, and resolved value.
    /// AutoMapper API compatible.
    /// </summary>
    void Condition(Func<TSource, TDestination, TMember, bool> condition);

    /// <summary>
    /// Apply a post-condition for this member mapping with ResolutionContext.
    /// AutoMapper API compatible.
    /// </summary>
    void Condition(Func<TSource, TDestination, TMember, ResolutionContext, bool> condition);

    /// <summary>
    /// Substitute a value when the source value is null.
    /// AutoMapper API compatible.
    /// </summary>
    void NullSubstitute(TMember substituteValue);

    /// <summary>
    /// Map this member using a custom value resolver type.
    /// The resolver is instantiated via ConstructServicesUsing() delegate.
    /// AutoMapper API compatible.
    /// </summary>
    void MapFrom<TValueResolver>()
        where TValueResolver : IValueResolver<TSource, TDestination, TMember>;

    /// <summary>
    /// Map this member using a custom value resolver instance.
    /// AutoMapper API compatible.
    /// </summary>
    void MapFrom(IValueResolver<TSource, TDestination, TMember> resolver);

    /// <summary>
    /// Supply a custom mapping order instead of what the .NET runtime returns.
    /// Properties without explicit order map first, then ordered properties execute
    /// from lowest to highest value. Useful when property setters have side effects.
    /// AutoMapper API compatible.
    /// </summary>
    /// <param name="mappingOrder">Mapping order value (null/no order maps first, then ascending)</param>
    void SetMappingOrder(int mappingOrder);
}
