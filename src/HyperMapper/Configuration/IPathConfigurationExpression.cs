using System.Linq.Expressions;

namespace HyperMapper.Configuration;

/// <summary>
/// v8.0.0: Configuration expression for ForPath() nested path mappings.
/// AutoMapper API compatible.
/// </summary>
public interface IPathConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    /// Map from a source member expression.
    /// </summary>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);

    /// <summary>
    /// Ignore this path during mapping.
    /// </summary>
    void Ignore();

    /// <summary>
    /// Apply a condition for this path mapping.
    /// </summary>
    void Condition(Func<TSource, bool> condition);
}
