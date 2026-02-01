using System.Linq.Expressions;

namespace HyperMapper.Internal;

internal class MemberMap
{
    public string DestinationMemberName { get; }
    public Delegate? SourceValueResolver { get; set; }

    /// <summary>
    /// Compiled resolver that avoids DynamicInvoke overhead.
    /// Used by the legacy path when execution plan is not available.
    /// </summary>
    public Func<object, object?>? CompiledResolver { get; set; }

    /// <summary>
    /// Pre-condition evaluated BEFORE value resolution.
    /// </summary>
    public Func<object, bool>? PreCondition { get; set; }

    /// <summary>
    /// v8.0.0: Post-condition evaluated AFTER value resolution.
    /// Parameters: (source, destination, resolvedValue) => bool
    /// </summary>
    public Func<object, object, object?, bool>? Condition { get; set; }

    /// <summary>
    /// v8.0.0: Post-condition with ResolutionContext.
    /// Parameters: (source, destination, resolvedValue, context) => bool
    /// </summary>
    public Func<object, object, object?, ResolutionContext, bool>? ConditionWithContext { get; set; }

    /// <summary>
    /// v8.0.0: Value to substitute when source value is null.
    /// </summary>
    public object? NullSubstitute { get; set; }

    /// <summary>
    /// v8.0.0: Indicates whether NullSubstitute has been configured.
    /// </summary>
    public bool HasNullSubstitute { get; set; }

    public bool Ignored { get; set; }

    /// <summary>
    /// The original Expression tree passed to MapFrom().
    /// Preserved to allow ExecutionPlanBuilder to integrate it into the compiled plan.
    /// </summary>
    public LambdaExpression? SourceExpression { get; set; }

    /// <summary>
    /// The return type of the MapFrom expression.
    /// Needed for type conversion handling in execution plans.
    /// </summary>
    public Type? SourceExpressionReturnType { get; set; }

    /// <summary>
    /// v10.0.0: Resolver that requires access to the destination object.
    /// MapFrom((src, dest) => ...) stores the function here.
    /// Parameters: (source, destination) => resolvedValue
    /// </summary>
    public Func<object, object, object?>? DestinationResolver { get; set; }

    /// <summary>
    /// v10.0.0: Indicates whether this mapping uses destination-dependent resolution.
    /// When true, DestinationResolver should be used instead of CompiledResolver.
    /// </summary>
    public bool HasDestinationParameter { get; set; }

    public MemberMap(string destinationMemberName)
    {
        DestinationMemberName = destinationMemberName;
    }
}
