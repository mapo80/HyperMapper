using System.Linq.Expressions;
using System.Reflection;

namespace HyperMapper.Internal;

/// <summary>
/// Result of building an execution plan, including metadata about
/// properties that need legacy handling.
/// </summary>
internal class ExecutionPlanResult
{
    public Func<object, object>? Plan { get; init; }
    public HashSet<string>? CollectionProperties { get; init; }
}

/// <summary>
/// Builds compiled execution plans for type mappings.
/// An execution plan is a single compiled delegate that maps source to destination
/// without any runtime reflection or iteration.
/// </summary>
internal class ExecutionPlanBuilder
{
    private readonly TypeMapRegistry? _registry;
    private readonly HashSet<(Type, Type)> _inProgress = new();
    private readonly Dictionary<(Type, Type), TypeMap?> _typeMapCache = new();

    /// <summary>
    /// Creates an ExecutionPlanBuilder with access to the registry for nested type lookups.
    /// </summary>
    public ExecutionPlanBuilder(TypeMapRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Creates an ExecutionPlanBuilder without registry access (backward compatibility).
    /// </summary>
    public ExecutionPlanBuilder() : this(null!)
    {
    }

    /// <summary>
    /// Builds a compiled execution plan for mapping between two types.
    /// This is a convenience wrapper around BuildExecutionPlanWithMetadata.
    /// </summary>
    /// <param name="sourceType">The source type</param>
    /// <param name="destType">The destination type</param>
    /// <param name="typeMap">Optional TypeMap with custom mappings</param>
    /// <returns>A compiled delegate that performs the mapping, or null if not possible</returns>
    public Func<object, object>? BuildExecutionPlan(Type sourceType, Type destType, TypeMap? typeMap)
    {
        var result = BuildExecutionPlanWithMetadata(sourceType, destType, typeMap);
        return result.Plan;
    }

    /// <summary>
    /// Builds a compiled execution plan for mapping between two types.
    /// Returns both the compiled plan and metadata about properties that need legacy handling.
    /// </summary>
    /// <param name="sourceType">The source type</param>
    /// <param name="destType">The destination type</param>
    /// <param name="typeMap">Optional TypeMap with custom mappings</param>
    /// <returns>An ExecutionPlanResult containing the plan and collection property metadata</returns>
    public ExecutionPlanResult BuildExecutionPlanWithMetadata(Type sourceType, Type destType, TypeMap? typeMap)
    {
        var emptyResult = new ExecutionPlanResult { Plan = null, CollectionProperties = null };

        // Skip complex cases for now (converters, value types)
        // v7.0.0: Also skip lambda converters - they need runtime execution via DynamicInvoke
        if (typeMap?.Converter != null || typeMap?.ConverterType != null || typeMap?.LambdaConverter != null)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has transforms - they need runtime execution
        if (typeMap != null && typeMap.Transforms.Count > 0)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has lifecycle hooks - they need runtime execution
        if (typeMap != null && (typeMap.HasBeforeMap || typeMap.HasAfterMap))
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has ForPath mappings - they need runtime execution
        if (typeMap != null && typeMap.PathMaps.Count > 0)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has MaxDepth - needs runtime depth tracking
        if (typeMap?.MaxDepth != null)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has PreserveReferences - needs runtime reference tracking
        if (typeMap?.PreserveReferences == true)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has IncludeMembers - needs runtime flattening
        if (typeMap?.IncludedMembers != null && typeMap.IncludedMembers.Count > 0)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has IncludeBase - needs runtime base config application
        if (typeMap?.IncludedBaseType != null)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has Include for derived types - needs runtime polymorphic dispatch
        if (typeMap?.IncludedDerivedTypes != null && typeMap.IncludedDerivedTypes.Count > 0)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has custom constructor - needs runtime constructor invocation
        if (typeMap?.HasCustomConstructor == true)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has ForCtorParam configuration - needs runtime constructor selection
        if (typeMap?.CtorParamMaps != null && typeMap.CtorParamMaps.Count > 0)
        {
            return emptyResult; // Will use legacy path
        }

        // v8.0.0: Skip if TypeMap has ForAllMembers or ForAllOtherMembers - these expand into MemberMaps
        // and may have complex configurations that need runtime handling
        if (typeMap?.ForAllMembersFactory != null || typeMap?.ForAllOtherMembersFactory != null)
        {
            return emptyResult; // Will use legacy path
        }

        if (destType.IsValueType)
        {
            return emptyResult; // Value types need special handling, use legacy path
        }

        var sourceParam = Expression.Parameter(typeof(object), "source");
        var statements = new List<Expression>();
        var variables = new List<ParameterExpression>();

        // Cast source to typed variable: TSource typedSource = (TSource)source;
        var typedSource = Expression.Variable(sourceType, "typedSource");
        variables.Add(typedSource);
        statements.Add(Expression.Assign(typedSource, Expression.Convert(sourceParam, sourceType)));

        // Create destination: TDest dest = new TDest();
        var destVar = Expression.Variable(destType, "dest");
        variables.Add(destVar);

        var destCtor = destType.GetConstructor(Type.EmptyTypes);
        if (destCtor == null)
        {
            // No parameterless constructor - can't build execution plan
            return emptyResult;
        }
        statements.Add(Expression.Assign(destVar, Expression.New(destCtor)));

        // Get property mappings
        var configuredMembers = typeMap?.ConfiguredMembers;
        var ignoredMembers = typeMap?.IgnoredMembers;
        var memberMaps = typeMap?.MemberMaps;

        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);
        var destProps = ReflectionCache.GetWritableProperties(destType);
        var destPropsDict = destProps.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // Track members already processed via MemberMaps
        var processedMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Track collection properties that need legacy handling (HYBRID EXECUTION)
        var collectionProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Generate member map assignments first (custom mappings with ForMember/MapFrom)
        if (memberMaps != null)
        {
            foreach (var memberMap in memberMaps)
            {
                if (memberMap.Ignored)
                {
                    processedMembers.Add(memberMap.DestinationMemberName);
                    continue;
                }

                // Members with preconditions need runtime evaluation - can't use execution plan
                if (memberMap.PreCondition != null)
                {
                    return emptyResult;
                }

                // v8.0.0: Members with Condition need runtime evaluation - can't use execution plan
                if (memberMap.Condition != null || memberMap.ConditionWithContext != null)
                {
                    return emptyResult;
                }

                // v8.0.0: Members with NullSubstitute need runtime evaluation - can't use execution plan
                if (memberMap.HasNullSubstitute)
                {
                    return emptyResult;
                }

                if (!destPropsDict.TryGetValue(memberMap.DestinationMemberName, out var destPropInfo))
                    continue;

                // If we have the original Expression tree, integrate it into the plan
                if (memberMap.SourceExpression != null)
                {
                    var memberAssignment = BuildMemberMapAssignment(
                        typedSource, destVar, destPropInfo, memberMap);

                    if (memberAssignment != null)
                    {
                        statements.Add(memberAssignment);
                        processedMembers.Add(memberMap.DestinationMemberName);
                        continue;
                    }
                    // If we couldn't generate the assignment, fall back to legacy path
                }

                // If we only have CompiledResolver without Expression, must use legacy path
                if (memberMap.CompiledResolver != null)
                {
                    return emptyResult;
                }
            }
        }

        // Generate convention-based property assignments
        bool canHandleNonCollectionProperties = true;

        foreach (var destProp in destProps)
        {
            // Skip if already processed via MemberMap
            if (processedMembers.Contains(destProp.Name))
                continue;

            // Skip if explicitly configured
            if (configuredMembers != null && configuredMembers.Contains(destProp.Name))
                continue;
            if (ignoredMembers != null && ignoredMembers.Contains(destProp.Name))
                continue;

            // Find source property (exact match first, then case-insensitive)
            PropertyInfo? sourceProp = null;
            if (!sourcePropsExact.TryGetValue(destProp.Name, out sourceProp))
            {
                sourcePropsCI.TryGetValue(destProp.Name, out sourceProp);
            }

            if (sourceProp == null)
                continue;

            // Check for special types that need runtime handling
            if (IsLazyType(sourceProp.PropertyType))
            {
                // Lazy<T> properties need special runtime handling
                canHandleNonCollectionProperties = false;
                break;
            }

            // Check for collections/dictionaries
            var sourceAnalysis = ReflectionCache.GetTypeAnalysis(sourceProp.PropertyType);
            var destAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);
            if (sourceAnalysis.IsEnumerable || destAnalysis.IsEnumerable ||
                sourceAnalysis.IsDictionary || destAnalysis.IsDictionary)
            {
                // v4.2.0: TRY to inline primitive collections first (List<string>, List<int>, etc.)
                var collectionAssignment = BuildPrimitiveCollectionAssignment(
                    typedSource, destVar, sourceProp, destProp);

                if (collectionAssignment != null)
                {
                    statements.Add(collectionAssignment);
                    processedMembers.Add(destProp.Name);
                    continue; // Successfully inlined!
                }

                // Fall back to HYBRID EXECUTION for complex collections
                collectionProperties.Add(destProp.Name);
                continue;
            }

            // Generate assignment expression
            var assignmentExpr = BuildPropertyAssignment(typedSource, destVar, sourceProp, destProp);
            if (assignmentExpr != null)
            {
                statements.Add(assignmentExpr);
            }
            else
            {
                // Can't handle this property via expression tree
                // If there's a source property but we couldn't generate an assignment,
                // it means we need special conversion logic (enum, DateTime, etc.)
                // Fall back to legacy path which handles all conversions
                canHandleNonCollectionProperties = false;
                break;
            }
        }

        // If we can't handle non-collection properties, fall back to full legacy path
        if (!canHandleNonCollectionProperties)
        {
            return emptyResult;
        }

        // If we only have collection properties (no simple/nested props compiled),
        // it's more efficient to use the legacy path entirely
        // statements has: cast (1) + new dest (1) + property assignments
        // If statements.Count <= 2, we have no actual property assignments
        if (statements.Count <= 2)
        {
            return emptyResult;
        }

        // Return boxed destination: return (object)dest;
        statements.Add(Expression.Convert(destVar, typeof(object)));

        var body = Expression.Block(variables, statements);
        var compiledPlan = Expression.Lambda<Func<object, object>>(body, sourceParam).Compile();

        return new ExecutionPlanResult
        {
            Plan = compiledPlan,
            CollectionProperties = collectionProperties.Count > 0 ? collectionProperties : null
        };
    }

    private Expression? BuildPropertyAssignment(
        ParameterExpression typedSource,
        ParameterExpression destVar,
        PropertyInfo sourceProp,
        PropertyInfo destProp)
    {
        try
        {
            var sourceValue = Expression.Property(typedSource, sourceProp);
            var destValue = Expression.Property(destVar, destProp);

            // Handle nullable types first (before IsAssignableFrom check)
            // because IsAssignableFrom(int?, int) returns true but Expression.Assign fails
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceProp.PropertyType);
            var underlyingDestType = Nullable.GetUnderlyingType(destProp.PropertyType);

            // Check if destination is nullable and source is the underlying type
            if (underlyingDestType != null && sourceProp.PropertyType == underlyingDestType)
            {
                // T -> T?: needs explicit Convert in Expression Trees
                var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                return Expression.Assign(destValue, converted);
            }

            // Check type compatibility for direct assignment
            if (destProp.PropertyType == sourceProp.PropertyType ||
                (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType) && underlyingDestType == null))
            {
                // Direct assignment: dest.Prop = source.Prop;
                return Expression.Assign(destValue, sourceValue);
            }

            if (underlyingSourceType != null && destProp.PropertyType == underlyingSourceType)
            {
                // T? -> T: dest.Prop = source.Prop.Value (only if HasValue)
                var hasValue = Expression.Property(sourceValue, "HasValue");
                var getValue = Expression.Property(sourceValue, "Value");
                var assignment = Expression.Assign(destValue, getValue);
                return Expression.IfThen(hasValue, assignment);
            }

            if (underlyingDestType != null)
            {
                // T1? -> T2? where T1 can convert to T2
                if (underlyingSourceType != null && underlyingDestType.IsAssignableFrom(underlyingSourceType))
                {
                    var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                    return Expression.Assign(destValue, converted);
                }

                // Other T -> T? cases (where T is compatible with underlying)
                if (underlyingDestType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                    return Expression.Assign(destValue, converted);
                }
            }

            // Handle simple type conversions (numeric, etc.)
            if (IsSimpleConversion(sourceProp.PropertyType, destProp.PropertyType))
            {
                var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                return Expression.Assign(destValue, converted);
            }

            // Check for nested complex objects
            var sourceAnalysis = ReflectionCache.GetTypeAnalysis(sourceProp.PropertyType);
            var destAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);

            // Skip collections and dictionaries (handled separately)
            if (sourceAnalysis.IsEnumerable || destAnalysis.IsEnumerable ||
                sourceAnalysis.IsDictionary || destAnalysis.IsDictionary)
            {
                return null;
            }

            // Handle nested complex objects - inline mapping instead of runtime recursion
            if (!sourceAnalysis.IsSimple && !destAnalysis.IsSimple)
            {
                return BuildNestedPropertyAssignment(typedSource, destVar, sourceProp, destProp, depth: 0);
            }

            // For other complex types, return null to use legacy path
            return null;
        }
        catch
        {
            // If expression building fails, return null to use legacy path
            return null;
        }
    }

    private static bool IsLazyType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>);
    }

    /// <summary>
    /// Gets a TypeMap from cache or registry, caching the result for reuse.
    /// </summary>
    private TypeMap? GetCachedTypeMap(Type sourceType, Type destType)
    {
        var key = (sourceType, destType);
        if (_typeMapCache.TryGetValue(key, out var cached))
            return cached;

        var result = _registry?.FindTypeMap(sourceType, destType);
        _typeMapCache[key] = result;
        return result;
    }

    private static bool IsSimpleConversion(Type sourceType, Type destType)
    {
        // Numeric conversions
        var numericTypes = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        if (numericTypes.Contains(sourceType) && numericTypes.Contains(destType))
            return true;

        // String conversions are handled separately
        return false;
    }

    /// <summary>
    /// Checks if an expression contains method calls (excluding property getters).
    /// Expressions with method calls need the legacy path for proper error handling.
    /// </summary>
    private static bool ContainsMethodCalls(Expression expression)
    {
        return expression switch
        {
            MethodCallExpression => true,
            MemberExpression me => me.Expression != null && ContainsMethodCalls(me.Expression),
            UnaryExpression ue => ContainsMethodCalls(ue.Operand),
            BinaryExpression be => ContainsMethodCalls(be.Left) || ContainsMethodCalls(be.Right),
            ConditionalExpression ce => ContainsMethodCalls(ce.Test) || ContainsMethodCalls(ce.IfTrue) || ContainsMethodCalls(ce.IfFalse),
            _ => false
        };
    }

    /// <summary>
    /// Builds an assignment expression for a MemberMap with SourceExpression.
    /// Rewrites the Expression to use typedSource instead of the original parameter.
    /// </summary>
    private Expression? BuildMemberMapAssignment(
        ParameterExpression typedSource,
        ParameterExpression destVar,
        PropertyInfo destProp,
        MemberMap memberMap)
    {
        try
        {
            var sourceExpr = memberMap.SourceExpression!;

            // Check if the expression contains method calls that could throw exceptions.
            // Such expressions need the legacy path which has try-catch error handling.
            if (ContainsMethodCalls(sourceExpr.Body))
            {
                return null;
            }

            var originalParam = sourceExpr.Parameters[0];

            // Rewrite the expression by replacing the original parameter with typedSource
            // E.g.: s => s.Sub.ProperName  becomes  typedSource.Sub.ProperName
            var rewriter = new ParameterReplacer(originalParam, typedSource);
            var rewrittenBody = rewriter.Visit(sourceExpr.Body);

            // Handle type conversion if needed
            var destValue = Expression.Property(destVar, destProp);
            var sourceReturnType = memberMap.SourceExpressionReturnType!;

            Expression valueExpr = rewrittenBody;

            // Type conversion if needed
            if (destProp.PropertyType != sourceReturnType)
            {
                var converted = BuildTypeConversion(rewrittenBody, sourceReturnType, destProp.PropertyType);
                if (converted == null)
                    return null; // Conversion not supported, use legacy path
                valueExpr = converted;
            }

            return Expression.Assign(destValue, valueExpr);
        }
        catch
        {
            // If expression building fails, return null to use legacy path
            return null;
        }
    }

    /// <summary>
    /// Builds assignment for a nested object property with null check.
    /// Generates inline mapping instead of recursive calls.
    /// </summary>
    private Expression? BuildNestedPropertyAssignment(
        Expression sourceObj,
        ParameterExpression destVar,
        PropertyInfo sourceProp,
        PropertyInfo destProp,
        int depth)
    {
        // Guard: max 20 levels to prevent stack overflow
        if (depth > 20) return null;

        // Cycle detection
        var key = (sourceProp.PropertyType, destProp.PropertyType);
        if (_inProgress.Contains(key)) return null;

        // Check if there's a TypeMap with a converter or transforms for this nested type
        // If so, we must use the legacy path to execute them
        var nestedTypeMap = GetCachedTypeMap(sourceProp.PropertyType, destProp.PropertyType);
        if (nestedTypeMap?.Converter != null || nestedTypeMap?.ConverterType != null)
        {
            return null; // Use legacy path which will execute the converter
        }
        // v8.0.0: Check for transforms on nested TypeMap
        if (nestedTypeMap != null && nestedTypeMap.Transforms.Count > 0)
        {
            return null; // Use legacy path which will execute transforms
        }

        // Must have parameterless constructor
        var destCtor = destProp.PropertyType.GetConstructor(Type.EmptyTypes);
        if (destCtor == null) return null;

        _inProgress.Add(key);
        try
        {
            var sourceValue = Expression.Property(sourceObj, sourceProp);
            var destValue = Expression.Property(destVar, destProp);

            // Build inline mapping expression
            var inlineMapping = BuildInlineNestedMapping(
                sourceValue, sourceProp.PropertyType, destProp.PropertyType, depth);

            if (inlineMapping == null) return null;

            // Null check for reference types and nullable structs
            var underlyingType = Nullable.GetUnderlyingType(sourceProp.PropertyType);
            if (!sourceProp.PropertyType.IsValueType)
            {
                // Reference type: if (source.Nested != null) { dest.Nested = <mapping>; }
                var nullCheck = Expression.NotEqual(sourceValue, Expression.Constant(null, sourceProp.PropertyType));
                var assignment = Expression.Assign(destValue, inlineMapping);
                return Expression.IfThen(nullCheck, assignment);
            }
            else if (underlyingType != null)
            {
                // Nullable struct (T?): if (source.Nested.HasValue) { dest.Nested = <mapping>; }
                var hasValue = Expression.Property(sourceValue, "HasValue");
                var assignment = Expression.Assign(destValue, inlineMapping);
                return Expression.IfThen(hasValue, assignment);
            }
            else
            {
                // Non-nullable value type: direct assignment
                return Expression.Assign(destValue, inlineMapping);
            }
        }
        finally
        {
            _inProgress.Remove(key);
        }
    }

    /// <summary>
    /// Builds an inline expression tree for mapping a nested object.
    /// Returns a block expression that creates and populates the destination.
    /// </summary>
    private Expression? BuildInlineNestedMapping(
        Expression sourceValue,
        Type sourceType,
        Type destType,
        int depth)
    {
        var destCtor = destType.GetConstructor(Type.EmptyTypes);
        if (destCtor == null) return null;

        var statements = new List<Expression>();
        var variables = new List<ParameterExpression>();

        // var nestedDest = new TDest();
        var nestedDest = Expression.Variable(destType, $"nested_{depth}");
        variables.Add(nestedDest);
        statements.Add(Expression.Assign(nestedDest, Expression.New(destCtor)));

        // Get properties
        var sourcePropsDict = ReflectionCache.GetReadablePropertiesDict(sourceType);
        var destProps = ReflectionCache.GetWritableProperties(destType);

        // Find TypeMap for this nested type (if exists)
        var nestedTypeMap = GetCachedTypeMap(sourceType, destType);

        foreach (var destProp in destProps)
        {
            // Skip if ignored in TypeMap
            if (nestedTypeMap?.IgnoredMembers?.Contains(destProp.Name) == true)
                continue;

            // Find matching source property
            if (!sourcePropsDict.TryGetValue(destProp.Name, out var sourceProp))
            {
                // Try case-insensitive match
                var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceType);
                if (!sourcePropsCI.TryGetValue(destProp.Name, out sourceProp))
                    continue;
            }

            // Build property assignment
            var propAssignment = BuildPropertyAssignmentForNested(
                sourceValue, nestedDest, sourceProp, destProp, depth);

            if (propAssignment != null)
            {
                statements.Add(propAssignment);
            }
            else
            {
                // Cannot handle this property inline - abort
                return null;
            }
        }

        // Return the nested destination object
        statements.Add(nestedDest);

        return Expression.Block(variables, statements);
    }

    /// <summary>
    /// Builds property assignment within a nested context.
    /// Handles simple types and recursively handles deeper nested objects.
    /// </summary>
    private Expression? BuildPropertyAssignmentForNested(
        Expression sourceObj,
        ParameterExpression destVar,
        PropertyInfo sourceProp,
        PropertyInfo destProp,
        int depth)
    {
        try
        {
            var sourceValue = Expression.Property(sourceObj, sourceProp);
            var destValue = Expression.Property(destVar, destProp);

            // Handle nullable types first
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceProp.PropertyType);
            var underlyingDestType = Nullable.GetUnderlyingType(destProp.PropertyType);

            // T -> T?: needs explicit Convert
            if (underlyingDestType != null && sourceProp.PropertyType == underlyingDestType)
            {
                var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                return Expression.Assign(destValue, converted);
            }

            // Direct assignment for same types or assignable types
            if (destProp.PropertyType == sourceProp.PropertyType ||
                (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType) && underlyingDestType == null))
            {
                return Expression.Assign(destValue, sourceValue);
            }

            // T? -> T: assign only if HasValue
            if (underlyingSourceType != null && destProp.PropertyType == underlyingSourceType)
            {
                var hasValue = Expression.Property(sourceValue, "HasValue");
                var getValue = Expression.Property(sourceValue, "Value");
                var assignment = Expression.Assign(destValue, getValue);
                return Expression.IfThen(hasValue, assignment);
            }

            // Other nullable conversions
            if (underlyingDestType != null)
            {
                if (underlyingSourceType != null && underlyingDestType.IsAssignableFrom(underlyingSourceType))
                {
                    var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                    return Expression.Assign(destValue, converted);
                }

                if (underlyingDestType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                    return Expression.Assign(destValue, converted);
                }
            }

            // Handle simple numeric conversions
            if (IsSimpleConversion(sourceProp.PropertyType, destProp.PropertyType))
            {
                var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                return Expression.Assign(destValue, converted);
            }

            // Check for nested complex objects
            var sourceAnalysis = ReflectionCache.GetTypeAnalysis(sourceProp.PropertyType);
            var destAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);

            // Skip collections and dictionaries (separate optimization)
            if (sourceAnalysis.IsEnumerable || destAnalysis.IsEnumerable ||
                sourceAnalysis.IsDictionary || destAnalysis.IsDictionary)
            {
                return null;
            }

            // Skip Lazy<T>
            if (IsLazyType(sourceProp.PropertyType))
            {
                return null;
            }

            // Recursively handle nested objects
            if (!sourceAnalysis.IsSimple && !destAnalysis.IsSimple)
            {
                return BuildNestedPropertyAssignment(sourceObj, destVar, sourceProp, destProp, depth + 1);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a type conversion expression between types.
    /// </summary>
    private Expression? BuildTypeConversion(Expression source, Type sourceType, Type destType)
    {
        // Same types or directly assignable
        if (sourceType == destType || destType.IsAssignableFrom(sourceType))
            return source;

        // Nullable handling: T -> T?
        var underlyingDest = Nullable.GetUnderlyingType(destType);
        if (underlyingDest != null)
        {
            if (sourceType == underlyingDest || underlyingDest.IsAssignableFrom(sourceType))
                return Expression.Convert(source, destType);
        }

        // Nullable handling: T? -> T
        var underlyingSource = Nullable.GetUnderlyingType(sourceType);
        if (underlyingSource != null && destType == underlyingSource)
        {
            // source.Value (assumes HasValue is handled by the lambda)
            return Expression.Property(source, "Value");
        }

        // Simple numeric conversions
        if (IsSimpleConversion(sourceType, destType))
            return Expression.Convert(source, destType);

        return null;
    }

    #region Primitive Collection Inlining (v4.2.0)

    /// <summary>
    /// Builds inline assignment for primitive collections (List&lt;string&gt;, List&lt;int&gt;, etc.)
    /// Uses the List&lt;T&gt;(IEnumerable&lt;T&gt;) constructor for efficient copying.
    /// </summary>
    private Expression? BuildPrimitiveCollectionAssignment(
        ParameterExpression typedSource,
        ParameterExpression destVar,
        PropertyInfo sourceProp,
        PropertyInfo destProp)
    {
        try
        {
            var sourceType = sourceProp.PropertyType;
            var destType = destProp.PropertyType;

            // Get element types
            var sourceElemType = GetCollectionElementType(sourceType);
            var destElemType = GetCollectionElementType(destType);

            if (sourceElemType == null || destElemType == null)
                return null;

            // Only handle same-type collections for now
            if (sourceElemType != destElemType)
                return null;

            // Only handle primitive/string collections
            if (!IsPrimitiveOrString(sourceElemType))
                return null;

            // Check if destination is List<T>
            if (!destType.IsGenericType || destType.GetGenericTypeDefinition() != typeof(List<>))
                return null;

            // Check if source is IEnumerable<T> (List<T>, IEnumerable<T>, ICollection<T>, etc.)
            var ienumerableType = typeof(IEnumerable<>).MakeGenericType(sourceElemType);
            if (!ienumerableType.IsAssignableFrom(sourceType))
                return null;

            var sourceValue = Expression.Property(typedSource, sourceProp);
            var destValue = Expression.Property(destVar, destProp);

            // List<T> has a constructor that takes IEnumerable<T> - use it for efficient copying!
            // This is much faster than iterating and adding elements one by one.
            var listCtor = destType.GetConstructor(new[] { ienumerableType });
            if (listCtor == null)
                return null;

            var emptyListCtor = destType.GetConstructor(Type.EmptyTypes);
            if (emptyListCtor == null)
                return null;

            // Build: dest.Tags = source.Tags != null ? new List<T>(source.Tags) : new List<T>();
            var nullCheck = Expression.NotEqual(sourceValue, Expression.Constant(null, sourceType));
            var newListFromSource = Expression.New(listCtor, sourceValue);
            var newEmptyList = Expression.New(emptyListCtor);

            var conditional = Expression.Condition(
                nullCheck,
                newListFromSource,
                newEmptyList);

            return Expression.Assign(destValue, conditional);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the element type of a collection type.
    /// </summary>
    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) ||
                genDef == typeof(IList<>) ||
                genDef == typeof(ICollection<>) ||
                genDef == typeof(IEnumerable<>) ||
                genDef == typeof(IReadOnlyList<>) ||
                genDef == typeof(IReadOnlyCollection<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a type is a primitive type or string (suitable for direct collection copying).
    /// </summary>
    private static bool IsPrimitiveOrString(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly) ||
               type == typeof(Guid) ||
               type == typeof(DateTimeOffset);
    }

    #endregion

    #region Collection Execution Plans (v4.3.0)

    /// <summary>
    /// Builds a compiled execution plan for mapping collections.
    /// Returns a typed delegate that maps source collection to destination collection
    /// without per-element MapInternal calls.
    /// </summary>
    public Func<object, object>? BuildCollectionExecutionPlan(
        Type sourceCollectionType,
        Type destCollectionType,
        Type sourceElementType,
        Type destElementType,
        TypeMap? elementTypeMap)
    {
        try
        {
            // Skip if converter is needed for elements
            // v7.0.0: Also skip lambda converters - they need runtime execution via DynamicInvoke
            if (elementTypeMap?.Converter != null || elementTypeMap?.ConverterType != null || elementTypeMap?.LambdaConverter != null)
                return null;

            // Check destination is List<T> (for now, extend later for Array/HashSet/etc.)
            if (!destCollectionType.IsGenericType)
                return null;

            var destGenericDef = destCollectionType.GetGenericTypeDefinition();
            var isList = destGenericDef == typeof(List<>) ||
                         destGenericDef == typeof(IList<>) ||
                         destGenericDef == typeof(ICollection<>) ||
                         destGenericDef == typeof(IEnumerable<>) ||
                         destGenericDef == typeof(IReadOnlyList<>) ||
                         destGenericDef == typeof(IReadOnlyCollection<>);

            if (!isList)
                return null;

            // Build element mapping lambda: item => new TDest { ... }
            var elementLambda = BuildElementMappingLambda(sourceElementType, destElementType, elementTypeMap);
            if (elementLambda == null)
                return null;

            // Build collection mapping: source => { var result = new List<T>(count); foreach(item) result.Add(elementLambda(item)); return result; }
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var statements = new List<Expression>();
            var variables = new List<ParameterExpression>();

            // Cast source to typed enumerable
            var sourceEnumType = typeof(IEnumerable<>).MakeGenericType(sourceElementType);
            var typedSource = Expression.Variable(sourceEnumType, "typedSource");
            variables.Add(typedSource);
            statements.Add(Expression.Assign(typedSource, Expression.Convert(sourceParam, sourceEnumType)));

            // Create result list
            var destListType = typeof(List<>).MakeGenericType(destElementType);
            var resultVar = Expression.Variable(destListType, "result");
            variables.Add(resultVar);

            // Try to get count for pre-allocation
            var countExpr = BuildCountExpression(sourceParam, sourceCollectionType, sourceElementType);
            var listCtorWithCapacity = destListType.GetConstructor(new[] { typeof(int) });
            var emptyListCtor = destListType.GetConstructor(Type.EmptyTypes);

            if (countExpr != null && listCtorWithCapacity != null)
            {
                statements.Add(Expression.Assign(resultVar, Expression.New(listCtorWithCapacity, countExpr)));
            }
            else
            {
                statements.Add(Expression.Assign(resultVar, Expression.New(emptyListCtor!)));
            }

            // Build loop (uses indexed for loop for List<T>, enumerator for others)
            var loopExpr = BuildCollectionLoopExpression(sourceParam, typedSource, resultVar, sourceCollectionType, sourceElementType, destElementType, elementLambda);
            statements.Add(loopExpr);

            // Return boxed result
            statements.Add(Expression.Convert(resultVar, typeof(object)));

            var body = Expression.Block(variables, statements);
            return Expression.Lambda<Func<object, object>>(body, sourceParam).Compile();
        }
        catch
        {
            // If expression building fails, return null to use legacy path
            return null;
        }
    }

    /// <summary>
    /// Builds a lambda expression for mapping a single element: item => new TDest { prop = item.prop, ... }
    /// </summary>
    private LambdaExpression? BuildElementMappingLambda(
        Type sourceElementType,
        Type destElementType,
        TypeMap? elementTypeMap)
    {
        // Skip if element has MemberMaps with MapFrom (need legacy path for those)
        if (elementTypeMap?.MemberMaps != null)
        {
            foreach (var memberMap in elementTypeMap.MemberMaps)
            {
                if (!memberMap.Ignored &&
                    (memberMap.SourceValueResolver != null ||
                     memberMap.CompiledResolver != null ||
                     memberMap.SourceExpression != null))
                {
                    // Element has custom mapping logic - fallback to legacy path
                    return null;
                }
            }
        }

        // Check for parameterless constructor
        var destCtor = destElementType.GetConstructor(Type.EmptyTypes);
        if (destCtor == null)
            return null;

        var itemParam = Expression.Parameter(sourceElementType, "item");
        var statements = new List<Expression>();
        var variables = new List<ParameterExpression>();

        // Create destination: var dest = new TDest();
        var destVar = Expression.Variable(destElementType, "elementDest");
        variables.Add(destVar);
        statements.Add(Expression.Assign(destVar, Expression.New(destCtor)));

        // Get properties
        var sourcePropsExact = ReflectionCache.GetReadablePropertiesDict(sourceElementType);
        var sourcePropsCI = ReflectionCache.GetReadablePropertiesDictCaseInsensitive(sourceElementType);
        var destProps = ReflectionCache.GetWritableProperties(destElementType);

        foreach (var destProp in destProps)
        {
            // Skip if ignored
            if (elementTypeMap?.IgnoredMembers?.Contains(destProp.Name) == true)
                continue;

            // Find source property
            if (!sourcePropsExact.TryGetValue(destProp.Name, out var sourceProp))
            {
                sourcePropsCI.TryGetValue(destProp.Name, out sourceProp);
            }

            if (sourceProp == null)
                continue;

            // Check for complex types that we can't inline
            var sourceAnalysis = ReflectionCache.GetTypeAnalysis(sourceProp.PropertyType);
            var destAnalysis = ReflectionCache.GetTypeAnalysis(destProp.PropertyType);

            // Skip collections and dictionaries in element properties (for now)
            if (sourceAnalysis.IsEnumerable || destAnalysis.IsEnumerable ||
                sourceAnalysis.IsDictionary || destAnalysis.IsDictionary)
            {
                return null; // Fallback to legacy
            }

            // Skip nested complex objects (for now - extend later)
            if (!sourceAnalysis.IsSimple && !destAnalysis.IsSimple)
            {
                return null; // Fallback to legacy
            }

            var sourceValue = Expression.Property(itemParam, sourceProp);
            var destValue = Expression.Property(destVar, destProp);

            // Handle type compatibility
            var underlyingDestType = Nullable.GetUnderlyingType(destProp.PropertyType);
            var underlyingSourceType = Nullable.GetUnderlyingType(sourceProp.PropertyType);

            if (destProp.PropertyType == sourceProp.PropertyType ||
                destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
            {
                statements.Add(Expression.Assign(destValue, sourceValue));
            }
            else if (underlyingDestType != null && sourceProp.PropertyType == underlyingDestType)
            {
                // T -> T?
                var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                statements.Add(Expression.Assign(destValue, converted));
            }
            else if (underlyingSourceType != null && destProp.PropertyType == underlyingSourceType)
            {
                // T? -> T (only assign if HasValue)
                var hasValue = Expression.Property(sourceValue, "HasValue");
                var getValue = Expression.Property(sourceValue, "Value");
                var assignment = Expression.Assign(destValue, getValue);
                statements.Add(Expression.IfThen(hasValue, assignment));
            }
            else if (IsSimpleConversion(sourceProp.PropertyType, destProp.PropertyType))
            {
                var converted = Expression.Convert(sourceValue, destProp.PropertyType);
                statements.Add(Expression.Assign(destValue, converted));
            }
            else
            {
                // Can't handle this property inline
                return null;
            }
        }

        statements.Add(destVar);

        var blockExpr = Expression.Block(variables, statements);
        return Expression.Lambda(blockExpr, itemParam);
    }

    /// <summary>
    /// Builds expression to get collection count for pre-allocation.
    /// </summary>
    private Expression? BuildCountExpression(Expression source, Type collectionType, Type elementType)
    {
        try
        {
            // Check if it's ICollection<T> (has Count property)
            var collectionInterface = typeof(ICollection<>).MakeGenericType(elementType);
            if (collectionInterface.IsAssignableFrom(collectionType))
            {
                var countProp = collectionInterface.GetProperty("Count")!;
                return Expression.Property(Expression.Convert(source, collectionInterface), countProp);
            }

            // Check for Array
            if (collectionType.IsArray)
            {
                return Expression.ArrayLength(Expression.Convert(source, collectionType));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the loop expression that maps each element.
    /// Uses indexed for loop for List&lt;T&gt; (faster than IEnumerator).
    /// Handles null elements by adding null to result (for reference types).
    /// </summary>
    private Expression BuildCollectionLoopExpression(
        Expression sourceParam,         // The original source parameter (object)
        ParameterExpression typedSource, // Typed as IEnumerable<T>
        ParameterExpression resultVar,
        Type sourceCollectionType,
        Type sourceElementType,
        Type destElementType,
        LambdaExpression elementLambda)
    {
        var addMethod = typeof(List<>).MakeGenericType(destElementType).GetMethod("Add")!;

        // Try to use indexed for loop for List<T> (faster than IEnumerator)
        var sourceListType = typeof(List<>).MakeGenericType(sourceElementType);
        if (sourceListType.IsAssignableFrom(sourceCollectionType) ||
            sourceCollectionType.IsArray)
        {
            return BuildIndexedForLoop(sourceParam, resultVar, sourceCollectionType, sourceElementType, destElementType, elementLambda, addMethod);
        }

        // Fall back to IEnumerator pattern for other IEnumerable types
        return BuildEnumeratorLoop(typedSource, resultVar, sourceElementType, destElementType, elementLambda, addMethod);
    }

    /// <summary>
    /// Builds an indexed for loop for List&lt;T&gt; or arrays (faster than IEnumerator).
    /// for (int i = 0; i &lt; list.Count; i++) { result.Add(map(list[i])); }
    /// </summary>
    private Expression BuildIndexedForLoop(
        Expression sourceParam,
        ParameterExpression resultVar,
        Type sourceCollectionType,
        Type sourceElementType,
        Type destElementType,
        LambdaExpression elementLambda,
        MethodInfo addMethod)
    {
        var sourceListType = typeof(List<>).MakeGenericType(sourceElementType);
        var isArray = sourceCollectionType.IsArray;

        var typedList = Expression.Variable(isArray ? sourceCollectionType : sourceListType, "typedList");
        var index = Expression.Variable(typeof(int), "i");
        var currentItem = Expression.Variable(sourceElementType, "currentItem");
        var breakLabel = Expression.Label("ForBreak");

        // Cast source to typed list/array
        var castToTyped = Expression.Assign(typedList, Expression.Convert(sourceParam, isArray ? sourceCollectionType : sourceListType));

        // Get count/length
        Expression countExpr;
        Expression getItemExpr;
        if (isArray)
        {
            countExpr = Expression.ArrayLength(typedList);
            getItemExpr = Expression.ArrayIndex(typedList, index);
        }
        else
        {
            var countProp = sourceListType.GetProperty("Count")!;
            countExpr = Expression.Property(typedList, countProp);
            var indexerProp = sourceListType.GetProperty("Item")!;
            getItemExpr = Expression.MakeIndex(typedList, indexerProp, new[] { index });
        }

        // INLINE the element lambda body
        var elementLambdaParam = elementLambda.Parameters[0];
        var rewriter = new ParameterReplacer(elementLambdaParam, currentItem);
        var inlinedMapping = rewriter.Visit(elementLambda.Body);

        // Build loop body with null check for reference types
        Expression loopBody;
        if (!sourceElementType.IsValueType)
        {
            var nullCheck = Expression.NotEqual(currentItem, Expression.Constant(null, sourceElementType));
            var addMapped = Expression.Call(resultVar, addMethod, inlinedMapping);
            var addNull = Expression.Call(resultVar, addMethod, Expression.Constant(null, destElementType));
            loopBody = Expression.IfThenElse(nullCheck, addMapped, addNull);
        }
        else
        {
            loopBody = Expression.Call(resultVar, addMethod, inlinedMapping);
        }

        // for (int i = 0; i < count; i++) { ... }
        var loop = Expression.Block(
            new[] { typedList, index, currentItem },
            castToTyped,
            Expression.Assign(index, Expression.Constant(0)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(index, countExpr),
                    Expression.Block(
                        Expression.Assign(currentItem, getItemExpr),
                        loopBody,
                        Expression.PostIncrementAssign(index)),
                    Expression.Break(breakLabel)),
                breakLabel));

        return loop;
    }

    /// <summary>
    /// Builds an IEnumerator-based loop for generic IEnumerable types.
    /// </summary>
    private Expression BuildEnumeratorLoop(
        ParameterExpression typedSource,
        ParameterExpression resultVar,
        Type sourceElementType,
        Type destElementType,
        LambdaExpression elementLambda,
        MethodInfo addMethod)
    {
        var enumeratorType = typeof(IEnumerator<>).MakeGenericType(sourceElementType);
        var getEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType(sourceElementType).GetMethod("GetEnumerator")!;
        var moveNextMethod = typeof(System.Collections.IEnumerator).GetMethod("MoveNext")!;
        var currentProp = enumeratorType.GetProperty("Current")!;

        var enumerator = Expression.Variable(enumeratorType, "enumerator");
        var currentItem = Expression.Variable(sourceElementType, "currentItem");
        var breakLabel = Expression.Label("LoopBreak");

        var getCurrentItem = Expression.Property(enumerator, currentProp);

        // INLINE the element lambda body
        var elementLambdaParam = elementLambda.Parameters[0];
        var rewriter = new ParameterReplacer(elementLambdaParam, currentItem);
        var inlinedMapping = rewriter.Visit(elementLambda.Body);

        Expression loopBody;
        if (!sourceElementType.IsValueType)
        {
            var nullCheck = Expression.NotEqual(currentItem, Expression.Constant(null, sourceElementType));
            var addMapped = Expression.Call(resultVar, addMethod, inlinedMapping);
            var addNull = Expression.Call(resultVar, addMethod, Expression.Constant(null, destElementType));
            loopBody = Expression.IfThenElse(nullCheck, addMapped, addNull);
        }
        else
        {
            loopBody = Expression.Call(resultVar, addMethod, inlinedMapping);
        }

        var loop = Expression.Block(
            new[] { enumerator, currentItem },
            Expression.Assign(enumerator, Expression.Call(typedSource, getEnumeratorMethod)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.Call(enumerator, moveNextMethod),
                    Expression.Block(
                        Expression.Assign(currentItem, getCurrentItem),
                        loopBody),
                    Expression.Break(breakLabel)),
                breakLabel));

        return loop;
    }

    #endregion
}
