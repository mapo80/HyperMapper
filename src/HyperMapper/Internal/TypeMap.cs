using System.Linq.Expressions;

namespace HyperMapper.Internal;

internal class TypeMap
{
    public Type SourceType { get; }
    public Type DestinationType { get; }
    public bool IsOpenGeneric { get; }

    private readonly List<MemberMap> _memberMaps = new();
    private object? _converter;
    private Type? _converterType;
    private Delegate? _lambdaConverter;

    // Pre-computed member sets to avoid allocations at mapping time
    private HashSet<string>? _configuredMembers;
    private HashSet<string>? _ignoredMembers;
    private bool _memberSetsComputed;

    // Execution plan for fast-path mapping
    private Func<object, object>? _executionPlan;

    // Collection properties that require legacy mapping after execution plan (hybrid execution)
    private HashSet<string>? _collectionPropertyNames;

    // v8.0.0: Value transformations per type
    private readonly Dictionary<Type, LambdaExpression> _transforms = new();

    // v8.0.0: Lifecycle hooks
    private Delegate? _beforeMap;
    private Delegate? _beforeMapWithContext;
    private Delegate? _afterMap;
    private Delegate? _afterMapWithContext;

    // v8.0.0: ForPath mappings
    private readonly List<PathMemberMap> _pathMaps = new();

    // v8.0.0: Depth limiting and reference preservation
    private int? _maxDepth;
    private bool _preserveReferences;

    // v8.0.0: IncludeMembers
    private readonly List<LambdaExpression> _includedMembers = new();

    // v8.0.0: Include/IncludeBase for inheritance
    private readonly List<(Type DerivedSource, Type DerivedDestination)> _includedDerivedTypes = new();
    private (Type BaseSource, Type BaseDestination)? _includedBaseType;

    // v8.0.0: Validation control
    private MemberList _validateMemberList = MemberList.Destination;

    // v8.0.0: Custom constructor
    private Delegate? _constructUsing;
    private Delegate? _constructUsingWithContext;

    // v8.0.0: Constructor parameter mapping
    private readonly List<CtorParamMap> _ctorParamMaps = new();

    // v8.0.0: Bulk member configuration
    private Func<string, MemberMap>? _forAllMembers;
    private Func<string, MemberMap>? _forAllOtherMembers;

    public TypeMap(Type sourceType, Type destinationType, bool isOpenGeneric = false)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
        IsOpenGeneric = isOpenGeneric;
    }

    public void AddMemberMap(MemberMap memberMap) => _memberMaps.Add(memberMap);
    public void SetConverter(object converter) => _converter = converter;
    public void SetConverterType(Type converterType) => _converterType = converterType;
    public void SetLambdaConverter(Delegate converter) => _lambdaConverter = converter;

    public IReadOnlyList<MemberMap> MemberMaps => _memberMaps;
    public object? Converter => _converter;
    public Type? ConverterType => _converterType;
    public Delegate? LambdaConverter => _lambdaConverter;

    /// <summary>
    /// Pre-computed set of configured member names (non-ignored).
    /// </summary>
    public HashSet<string>? ConfiguredMembers
    {
        get
        {
            EnsureMemberSetsComputed();
            return _configuredMembers;
        }
    }

    /// <summary>
    /// Pre-computed set of ignored member names.
    /// </summary>
    public HashSet<string>? IgnoredMembers
    {
        get
        {
            EnsureMemberSetsComputed();
            return _ignoredMembers;
        }
    }

    /// <summary>
    /// Compiled execution plan for fast mapping.
    /// </summary>
    public Func<object, object>? ExecutionPlan => _executionPlan;

    /// <summary>
    /// Whether an execution plan has been built.
    /// </summary>
    public bool HasExecutionPlan => _executionPlan != null;

    /// <summary>
    /// Property names that contain collections and need legacy mapping.
    /// Only relevant when HasExecutionPlan is true (hybrid execution).
    /// </summary>
    public HashSet<string>? CollectionProperties => _collectionPropertyNames;

    /// <summary>
    /// Sets the compiled execution plan.
    /// </summary>
    public void SetExecutionPlan(Func<object, object> plan)
    {
        _executionPlan = plan;
    }

    /// <summary>
    /// Sets the collection property names that need legacy mapping after execution plan.
    /// </summary>
    public void SetCollectionProperties(HashSet<string> propertyNames)
    {
        _collectionPropertyNames = propertyNames;
    }

    /// <summary>
    /// Finalizes configuration by pre-computing member sets.
    /// Should be called after all member maps are added.
    /// </summary>
    public void FinalizeConfiguration()
    {
        // v8.0.0: Expand ForAllMembers and ForAllOtherMembers into actual MemberMaps
        ExpandBulkMemberConfigurations();

        EnsureMemberSetsComputed();
    }

    /// <summary>
    /// v8.0.0: Expands ForAllMembers() and ForAllOtherMembers() into MemberMaps.
    /// </summary>
    private void ExpandBulkMemberConfigurations()
    {
        // Skip for open generic types - can't enumerate properties
        if (IsOpenGeneric) return;

        // Get all writable destination properties
        var destProperties = DestinationType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Get explicitly configured member names (from ForMember calls)
        var explicitlyConfigured = new HashSet<string>(
            _memberMaps.Select(m => m.DestinationMemberName),
            StringComparer.OrdinalIgnoreCase);

        // ForAllMembers: Apply to ALL destination members
        if (_forAllMembers != null)
        {
            foreach (var propName in destProperties)
            {
                // Skip if explicitly configured (ForMember takes precedence)
                if (explicitlyConfigured.Contains(propName)) continue;

                var memberMap = _forAllMembers(propName);
                _memberMaps.Add(memberMap);
            }
        }
        // ForAllOtherMembers: Apply only to NOT explicitly configured members
        else if (_forAllOtherMembers != null)
        {
            foreach (var propName in destProperties)
            {
                // Only apply to members NOT explicitly configured
                if (explicitlyConfigured.Contains(propName)) continue;

                var memberMap = _forAllOtherMembers(propName);
                _memberMaps.Add(memberMap);
            }
        }
    }

    private void EnsureMemberSetsComputed()
    {
        if (_memberSetsComputed) return;

        if (_memberMaps.Count > 0)
        {
            _configuredMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _ignoredMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in _memberMaps)
            {
                if (m.Ignored)
                    _ignoredMembers.Add(m.DestinationMemberName);
                else
                    _configuredMembers.Add(m.DestinationMemberName);
            }
        }

        _memberSetsComputed = true;
    }

    #region v8.0.0: Value Transformations

    /// <summary>
    /// Adds a value transformation for a specific type.
    /// </summary>
    public void AddTransform(Type type, LambdaExpression transformer)
    {
        _transforms[type] = transformer;
    }

    /// <summary>
    /// Gets all registered transforms.
    /// </summary>
    public IReadOnlyDictionary<Type, LambdaExpression> Transforms => _transforms;

    /// <summary>
    /// Gets transform for a specific type, if configured.
    /// </summary>
    public LambdaExpression? GetTransform(Type type)
    {
        return _transforms.TryGetValue(type, out var transform) ? transform : null;
    }

    #endregion

    #region v8.0.0: Lifecycle Hooks

    public void SetBeforeMap(Delegate beforeMap) => _beforeMap = beforeMap;
    public void SetBeforeMapWithContext(Delegate beforeMap) => _beforeMapWithContext = beforeMap;
    public void SetAfterMap(Delegate afterMap) => _afterMap = afterMap;
    public void SetAfterMapWithContext(Delegate afterMap) => _afterMapWithContext = afterMap;

    public Delegate? BeforeMap => _beforeMap;
    public Delegate? BeforeMapWithContext => _beforeMapWithContext;
    public Delegate? AfterMap => _afterMap;
    public Delegate? AfterMapWithContext => _afterMapWithContext;

    public bool HasBeforeMap => _beforeMap != null || _beforeMapWithContext != null;
    public bool HasAfterMap => _afterMap != null || _afterMapWithContext != null;

    #endregion

    #region v8.0.0: ForPath

    public void AddPathMap(PathMemberMap pathMap) => _pathMaps.Add(pathMap);
    public IReadOnlyList<PathMemberMap> PathMaps => _pathMaps;

    #endregion

    #region v8.0.0: MaxDepth and PreserveReferences

    public void SetMaxDepth(int depth) => _maxDepth = depth;
    public int? MaxDepth => _maxDepth;

    public void SetPreserveReferences() => _preserveReferences = true;
    public bool PreserveReferences => _preserveReferences;

    #endregion

    #region v8.0.0: IncludeMembers

    public void AddIncludedMember(LambdaExpression memberExpression) => _includedMembers.Add(memberExpression);
    public IReadOnlyList<LambdaExpression> IncludedMembers => _includedMembers;

    #endregion

    #region v8.0.0: Include/IncludeBase

    public void AddIncludedDerivedType(Type derivedSource, Type derivedDestination)
    {
        _includedDerivedTypes.Add((derivedSource, derivedDestination));
    }

    public void SetIncludedBaseType(Type baseSource, Type baseDestination)
    {
        _includedBaseType = (baseSource, baseDestination);
    }

    public IReadOnlyList<(Type DerivedSource, Type DerivedDestination)> IncludedDerivedTypes => _includedDerivedTypes;
    public (Type BaseSource, Type BaseDestination)? IncludedBaseType => _includedBaseType;

    #endregion

    #region v8.0.0: ValidateMemberList

    public void SetValidateMemberList(MemberList memberList) => _validateMemberList = memberList;
    public MemberList ValidateMemberList => _validateMemberList;

    #endregion

    #region v8.0.0: Constructor Mapping

    public void SetConstructUsing(Delegate constructor) => _constructUsing = constructor;
    public void SetConstructUsingWithContext(Delegate constructor) => _constructUsingWithContext = constructor;
    public Delegate? ConstructUsing => _constructUsing;
    public Delegate? ConstructUsingWithContext => _constructUsingWithContext;
    public bool HasCustomConstructor => _constructUsing != null || _constructUsingWithContext != null;

    public void AddCtorParamMap(CtorParamMap ctorParamMap) => _ctorParamMaps.Add(ctorParamMap);
    public IReadOnlyList<CtorParamMap> CtorParamMaps => _ctorParamMaps;

    #endregion

    #region v8.0.0: Bulk Configuration

    public void SetForAllMembers(Func<string, MemberMap> factory) => _forAllMembers = factory;
    public void SetForAllOtherMembers(Func<string, MemberMap> factory) => _forAllOtherMembers = factory;
    public Func<string, MemberMap>? ForAllMembersFactory => _forAllMembers;
    public Func<string, MemberMap>? ForAllOtherMembersFactory => _forAllOtherMembers;

    #endregion
}
