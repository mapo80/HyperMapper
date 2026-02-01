using System.Linq.Expressions;

namespace HyperMapper.Configuration;

/// <summary>
/// v8.0.0: Configuration expression for ForCtorParam() constructor parameter mappings.
/// AutoMapper API compatible.
/// </summary>
public interface ICtorParamConfigurationExpression<TSource>
{
    /// <summary>
    /// Map from a source member expression.
    /// </summary>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
}
