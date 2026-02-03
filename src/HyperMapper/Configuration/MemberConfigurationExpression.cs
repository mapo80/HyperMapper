using System.Linq.Expressions;
using HyperMapper.Internal;

namespace HyperMapper.Configuration;

internal class MemberConfigurationExpression<TSource, TDestination, TMember>
    : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    private readonly string _memberName;
    private Delegate? _sourceValueResolver;
    private Func<object, object?>? _compiledResolver;
    private Func<object, bool>? _preCondition;
    private Func<object, object, object?, bool>? _condition;  // v8.0.0: Post-mapping condition
    private object? _nullSubstitute;  // v8.0.0: Null substitution value
    private bool _hasNullSubstitute;  // v8.0.0: Flag for null substitute
    private bool _ignored;

    // Preserve the original Expression tree for fast-path execution plans
    private LambdaExpression? _sourceExpression;
    private Type? _sourceExpressionReturnType;

    public MemberConfigurationExpression(string memberName)
    {
        _memberName = memberName;
    }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        // Preserve the Expression tree BEFORE compiling
        // This allows ExecutionPlanBuilder to integrate it into the compiled plan
        _sourceExpression = sourceMember;
        _sourceExpressionReturnType = typeof(TSourceMember);

        // Compile for backward compatibility (used by legacy path)
        var compiled = sourceMember.Compile();
        _sourceValueResolver = compiled;
        // Create a typed wrapper to avoid DynamicInvoke
        _compiledResolver = src => compiled((TSource)src);
    }

    public void MapFrom<TResult>(Func<TSource, TDestination, TResult> mappingFunction)
    {
        // v10.0.0: Store the mapping function that requires destination parameter.
        // This will be executed AFTER the destination is populated with other properties.
        // Leave _sourceExpression = null to force legacy path.
        _hasDestinationParameter = true;
        _destinationResolver = (src, dest) => mappingFunction((TSource)src, (TDestination)dest);
        // Also provide fallback for places that don't support destination-dependent resolution
        _sourceValueResolver = new Func<TSource, object?>(src => mappingFunction(src, default!));
        _compiledResolver = src => mappingFunction((TSource)src, default!);
    }

    private bool _hasDestinationParameter;
    private Func<object, object, object?>? _destinationResolver;

    public void Ignore()
    {
        _ignored = true;
    }

    public void PreCondition(Func<TSource, bool> condition)
    {
        _preCondition = obj => condition((TSource)obj);
    }

    public void Condition(Func<TSource, TDestination, TMember, bool> condition)
    {
        _condition = (src, dest, val) => condition((TSource)src, (TDestination)dest, (TMember)val!);
    }

    public void Condition(Func<TSource, TDestination, TMember, ResolutionContext, bool> condition)
    {
        // Note: ResolutionContext will be passed at runtime via Mapper
        // For now, we wrap this to be compatible with the stored delegate signature
        // The ResolutionContext parameter will be injected at mapping time
        _condition = (src, dest, val) => true; // Placeholder - will be replaced with full implementation
        // Store the actual condition for later use
        _conditionWithContext = condition;
    }

    private Func<TSource, TDestination, TMember, ResolutionContext, bool>? _conditionWithContext;

    // v12.0.0: IValueResolver support
    private Type? _resolverType;
    private object? _resolverInstance;

    // v12.1.0: Mapping order support
    private int? _mappingOrder;

    public void NullSubstitute(TMember substituteValue)
    {
        _nullSubstitute = substituteValue;
        _hasNullSubstitute = true;
    }

    public void MapFrom<TValueResolver>()
        where TValueResolver : IValueResolver<TSource, TDestination, TMember>
    {
        _resolverType = typeof(TValueResolver);
        _resolverInstance = null;
        // Clear other mapping mechanisms
        _sourceExpression = null;
        _sourceValueResolver = null;
        _compiledResolver = null;
        _hasDestinationParameter = false;
        _destinationResolver = null;
    }

    public void MapFrom(IValueResolver<TSource, TDestination, TMember> resolver)
    {
        _resolverType = resolver.GetType();
        _resolverInstance = resolver;
        // Clear other mapping mechanisms
        _sourceExpression = null;
        _sourceValueResolver = null;
        _compiledResolver = null;
        _hasDestinationParameter = false;
        _destinationResolver = null;
    }

    public void SetMappingOrder(int mappingOrder)
    {
        _mappingOrder = mappingOrder;
    }

    internal MemberMap ToMemberMap()
    {
        return new MemberMap(_memberName)
        {
            SourceValueResolver = _sourceValueResolver,
            CompiledResolver = _compiledResolver,
            PreCondition = _preCondition,
            Condition = _condition,
            ConditionWithContext = _conditionWithContext != null
                ? (src, dest, val, ctx) => _conditionWithContext((TSource)src, (TDestination)dest, (TMember)val!, ctx)
                : null,
            NullSubstitute = _nullSubstitute,
            HasNullSubstitute = _hasNullSubstitute,
            Ignored = _ignored,
            // Pass the Expression tree to the MemberMap for execution plan integration
            SourceExpression = _sourceExpression,
            SourceExpressionReturnType = _sourceExpressionReturnType,
            // v10.0.0: Pass destination-dependent resolver
            HasDestinationParameter = _hasDestinationParameter,
            DestinationResolver = _destinationResolver,
            // v12.0.0: Pass IValueResolver configuration
            ResolverType = _resolverType,
            ResolverInstance = _resolverInstance,
            // v12.1.0: Pass mapping order configuration
            MappingOrder = _mappingOrder
        };
    }
}
