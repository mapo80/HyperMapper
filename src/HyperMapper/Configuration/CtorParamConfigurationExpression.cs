using System.Linq.Expressions;
using HyperMapper.Internal;

namespace HyperMapper.Configuration;

/// <summary>
/// v8.0.0: Implementation of ForCtorParam() configuration.
/// </summary>
internal class CtorParamConfigurationExpression<TSource> : ICtorParamConfigurationExpression<TSource>
{
    private readonly string _parameterName;
    private LambdaExpression? _sourceExpression;
    private Delegate? _sourceValueResolver;

    public CtorParamConfigurationExpression(string parameterName)
    {
        _parameterName = parameterName;
    }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        _sourceExpression = sourceMember;
        _sourceValueResolver = sourceMember.Compile();
    }

    internal CtorParamMap ToCtorParamMap()
    {
        return new CtorParamMap(_parameterName)
        {
            SourceExpression = _sourceExpression,
            SourceValueResolver = _sourceValueResolver
        };
    }
}
