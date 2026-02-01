using System.Linq.Expressions;

namespace HyperMapper.Internal;

/// <summary>
/// v8.0.0: Stores ForCtorParam() configuration for constructor parameter mappings.
/// </summary>
internal class CtorParamMap
{
    /// <summary>
    /// Name of the constructor parameter.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// The source expression for mapping.
    /// </summary>
    public LambdaExpression? SourceExpression { get; set; }

    /// <summary>
    /// Compiled source value resolver.
    /// </summary>
    public Delegate? SourceValueResolver { get; set; }

    public CtorParamMap(string parameterName)
    {
        ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
    }
}
