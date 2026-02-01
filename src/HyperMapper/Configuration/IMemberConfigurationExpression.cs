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
}
