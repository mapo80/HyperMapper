using System.Collections.Concurrent;

namespace HyperMapper.Internal;

internal class TypeMapRegistry
{
    private readonly Dictionary<(Type Source, Type Dest), TypeMap> _typeMaps = new();
    private readonly List<TypeMap> _openGenericMaps = new();

    // Cache for convention-only execution plans (no explicit TypeMap)
    private readonly ConcurrentDictionary<(Type, Type), Func<object, object>?> _conventionPlans = new();

    // Cache for collection execution plans (v4.3.0)
    private readonly ConcurrentDictionary<(Type, Type, Type, Type), Func<object, object>?> _collectionPlans = new();

    public void Register(TypeMap typeMap)
    {
        if (typeMap.IsOpenGeneric)
        {
            _openGenericMaps.Add(typeMap);
        }
        else
        {
            _typeMaps[(typeMap.SourceType, typeMap.DestinationType)] = typeMap;
        }
    }

    public TypeMap? FindTypeMap(Type sourceType, Type destType)
    {
        // Try exact match first
        if (_typeMaps.TryGetValue((sourceType, destType), out var exactMap))
        {
            return exactMap;
        }

        // v8.0.0: Try polymorphic dispatch via Include() - find base mapping that includes this derived type
        var polymorphicMap = FindPolymorphicTypeMap(sourceType, destType);
        if (polymorphicMap != null)
        {
            return polymorphicMap;
        }

        // Try open generic maps
        foreach (var openMap in _openGenericMaps)
        {
            if (MatchesOpenGeneric(sourceType, destType, openMap))
            {
                return openMap;
            }
        }

        return null;
    }

    /// <summary>
    /// v8.0.0: Finds a TypeMap for derived types using polymorphic dispatch.
    /// When a base mapping has Include() for derived types, this finds and returns
    /// the appropriate derived mapping (or creates one based on base configuration).
    /// </summary>
    private TypeMap? FindPolymorphicTypeMap(Type actualSourceType, Type requestedDestType)
    {
        // Look for base mappings that include this derived type
        foreach (var kvp in _typeMaps)
        {
            var baseMap = kvp.Value;
            var (baseSource, baseDest) = kvp.Key;

            // Check if requested mapping matches the base mapping (e.g., Vehicle -> VehicleDto)
            // but the actual source type is a derived type (e.g., Car)
            if (baseDest == requestedDestType && baseSource.IsAssignableFrom(actualSourceType) && baseSource != actualSourceType)
            {
                // Check if this base map includes a derived mapping for the actual source type
                foreach (var (derivedSource, derivedDest) in baseMap.IncludedDerivedTypes)
                {
                    // Find the most specific match for the actual source type
                    if (derivedSource == actualSourceType || derivedSource.IsAssignableFrom(actualSourceType))
                    {
                        // Try to find explicit mapping for derived types
                        if (_typeMaps.TryGetValue((derivedSource, derivedDest), out var derivedMap))
                        {
                            // If the derived map also has includes, recursively check for more specific match
                            if (derivedSource != actualSourceType && derivedMap.IncludedDerivedTypes.Count > 0)
                            {
                                var moreSpecific = FindPolymorphicTypeMapForDerived(actualSourceType, derivedMap);
                                if (moreSpecific != null)
                                    return moreSpecific;
                            }
                            return derivedMap;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively finds a more specific derived TypeMap within a derived mapping chain.
    /// </summary>
    private TypeMap? FindPolymorphicTypeMapForDerived(Type actualSourceType, TypeMap baseMap)
    {
        foreach (var (derivedSource, derivedDest) in baseMap.IncludedDerivedTypes)
        {
            if (derivedSource == actualSourceType || derivedSource.IsAssignableFrom(actualSourceType))
            {
                if (_typeMaps.TryGetValue((derivedSource, derivedDest), out var derivedMap))
                {
                    // Continue recursively if there are more includes
                    if (derivedSource != actualSourceType && derivedMap.IncludedDerivedTypes.Count > 0)
                    {
                        var moreSpecific = FindPolymorphicTypeMapForDerived(actualSourceType, derivedMap);
                        if (moreSpecific != null)
                            return moreSpecific;
                    }
                    return derivedMap;
                }
            }
        }
        return null;
    }

    private static bool MatchesOpenGeneric(Type sourceType, Type destType, TypeMap openMap)
    {
        if (!sourceType.IsGenericType || !destType.IsGenericType)
            return false;

        var sourceGenericDef = sourceType.GetGenericTypeDefinition();
        var destGenericDef = destType.GetGenericTypeDefinition();

        // Check if source implements the open generic source type
        if (openMap.SourceType.IsInterface)
        {
            var matchingInterface = sourceType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openMap.SourceType);

            if (matchingInterface == null && !(sourceType.IsGenericType && sourceGenericDef == openMap.SourceType))
                return false;
        }
        else if (sourceGenericDef != openMap.SourceType)
        {
            return false;
        }

        return destGenericDef == openMap.DestinationType;
    }

    public void Validate()
    {
        var errors = new List<string>();

        foreach (var typeMap in _typeMaps.Values)
        {
            ValidateTypeMap(typeMap, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Mapping configuration validation failed:\n{string.Join("\n", errors)}");
        }
    }

    private void ValidateTypeMap(TypeMap typeMap, List<string> errors)
    {
        // v8.0.0: Skip validation if MemberList.None is set
        if (typeMap.ValidateMemberList == MemberList.None)
            return;

        // Skip open generic types - they can't be validated statically
        if (typeMap.IsOpenGeneric)
            return;

        var configuredDestMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredSourceMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Collect configured members from MemberMaps
        foreach (var memberMap in typeMap.MemberMaps)
        {
            configuredDestMembers.Add(memberMap.DestinationMemberName);

            // Try to extract source member name from the source expression
            if (memberMap.SourceExpression != null)
            {
                var sourceMemberName = GetMemberNameFromExpression(memberMap.SourceExpression);
                if (sourceMemberName != null)
                {
                    configuredSourceMembers.Add(sourceMemberName);
                }
            }
        }

        // Collect configured members from PathMaps
        foreach (var pathMap in typeMap.PathMaps)
        {
            // Add the root path member (first part of the path)
            if (pathMap.PathSegments.Count > 0)
            {
                configuredDestMembers.Add(pathMap.PathSegments[0]);
            }
        }

        // Collect configured members from IncludeMembers
        foreach (var includedMember in typeMap.IncludedMembers)
        {
            var memberName = GetMemberNameFromExpression(includedMember);
            if (memberName != null)
            {
                configuredSourceMembers.Add(memberName);
            }
        }

        if (typeMap.ValidateMemberList == MemberList.Destination)
        {
            ValidateDestinationMembers(typeMap, configuredDestMembers, errors);
        }
        else if (typeMap.ValidateMemberList == MemberList.Source)
        {
            ValidateSourceMembers(typeMap, configuredSourceMembers, configuredDestMembers, errors);
        }
    }

    private void ValidateDestinationMembers(TypeMap typeMap, HashSet<string> configuredDestMembers, List<string> errors)
    {
        // Get all writable destination properties
        var destProperties = typeMap.DestinationType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .ToList();

        // Get all source property names for convention matching
        var sourcePropertyNames = typeMap.SourceType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check each destination property
        foreach (var destProp in destProperties)
        {
            // Skip if explicitly configured
            if (configuredDestMembers.Contains(destProp))
                continue;

            // Skip if there's a matching source property (convention mapping)
            if (sourcePropertyNames.Contains(destProp))
                continue;

            // Check if there's a nested type property that matches via flattening
            // e.g., AddressStreet on dest matches Address.Street on source
            if (HasFlatteningMatch(typeMap.SourceType, destProp))
                continue;

            // Unmapped member found
            errors.Add($"Unmapped member '{destProp}' on destination type " +
                       $"'{typeMap.DestinationType.Name}' from source type '{typeMap.SourceType.Name}'. " +
                       "Configure with .ForMember() or .ForPath(), or call .Ignore() to skip validation.");
        }
    }

    private void ValidateSourceMembers(TypeMap typeMap, HashSet<string> configuredSourceMembers,
        HashSet<string> configuredDestMembers, List<string> errors)
    {
        // Get all readable source properties
        var sourceProperties = typeMap.SourceType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => p.Name)
            .ToList();

        // Get all destination property names for convention matching
        var destPropertyNames = typeMap.DestinationType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check each source property
        foreach (var sourceProp in sourceProperties)
        {
            // Skip if explicitly configured
            if (configuredSourceMembers.Contains(sourceProp))
                continue;

            // Skip if there's a matching destination property (convention mapping)
            if (destPropertyNames.Contains(sourceProp))
                continue;

            // Skip if used in an IncludeMembers for flattening
            // The entire object is used for its properties

            // Unmapped member found
            errors.Add($"Unmapped source member '{sourceProp}' on source type " +
                       $"'{typeMap.SourceType.Name}' to destination type '{typeMap.DestinationType.Name}'. " +
                       "Configure with .ForMember(dest, opt => opt.MapFrom(src => src.Property)), " +
                       "use .IncludeMembers(), or set .ValidateMemberList(MemberList.Destination).");
        }
    }

    private static bool HasFlatteningMatch(Type sourceType, string destPropertyName)
    {
        // Check if destPropertyName like "AddressStreet" can be flattened from "Address.Street"
        var sourceProperties = sourceType
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        foreach (var sourceProp in sourceProperties)
        {
            if (destPropertyName.StartsWith(sourceProp.Name, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = destPropertyName.Substring(sourceProp.Name.Length);
                if (remainder.Length > 0)
                {
                    // Check if the remainder matches a property on the nested type
                    var nestedProp = sourceProp.PropertyType
                        .GetProperty(remainder, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (nestedProp != null && nestedProp.CanRead)
                        return true;

                    // Recursively check deeper nesting
                    if (HasFlatteningMatch(sourceProp.PropertyType, remainder))
                        return true;
                }
            }
        }

        return false;
    }

    private static string? GetMemberNameFromExpression(System.Linq.Expressions.LambdaExpression expression)
    {
        if (expression.Body is System.Linq.Expressions.MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        if (expression.Body is System.Linq.Expressions.UnaryExpression unaryExpr &&
            unaryExpr.Operand is System.Linq.Expressions.MemberExpression innerMemberExpr)
        {
            return innerMemberExpr.Member.Name;
        }
        return null;
    }

    /// <summary>
    /// Finalizes all registered type maps by pre-computing member sets.
    /// Should be called after all maps are configured.
    /// </summary>
    public void FinalizeAll()
    {
        foreach (var typeMap in _typeMaps.Values)
        {
            typeMap.FinalizeConfiguration();
        }

        foreach (var typeMap in _openGenericMaps)
        {
            typeMap.FinalizeConfiguration();
        }
    }

    /// <summary>
    /// Builds execution plans for all registered type maps.
    /// Should be called after FinalizeAll().
    /// </summary>
    public void BuildAllExecutionPlans()
    {
        // Pass this registry to enable nested type lookups
        var builder = new ExecutionPlanBuilder(this);

        foreach (var typeMap in _typeMaps.Values)
        {
            if (!typeMap.HasExecutionPlan)
            {
                var result = builder.BuildExecutionPlanWithMetadata(
                    typeMap.SourceType, typeMap.DestinationType, typeMap);

                if (result.Plan != null)
                {
                    typeMap.SetExecutionPlan(result.Plan);

                    // Track collection properties for hybrid execution
                    if (result.CollectionProperties != null)
                    {
                        typeMap.SetCollectionProperties(result.CollectionProperties);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets or creates a convention-based execution plan for types without explicit mapping.
    /// </summary>
    public Func<object, object>? GetConventionPlan(Type sourceType, Type destType)
    {
        return _conventionPlans.GetOrAdd((sourceType, destType), key =>
        {
            // Pass this registry to enable nested type lookups
            var builder = new ExecutionPlanBuilder(this);
            return builder.BuildExecutionPlan(key.Item1, key.Item2, null);
        });
    }

    /// <summary>
    /// Gets or creates a collection execution plan for mapping collections of elements.
    /// This enables fast path for List&lt;T&gt; mappings without per-element MapInternal calls.
    /// </summary>
    public Func<object, object>? GetCollectionPlan(
        Type sourceCollectionType,
        Type destCollectionType,
        Type sourceElementType,
        Type destElementType)
    {
        // v8.0.0: Skip collection execution plan if element type has Include mappings
        // This requires per-element polymorphic dispatch which can't be done at compile time
        var elementTypeMap = FindTypeMap(sourceElementType, destElementType);
        if (elementTypeMap?.IncludedDerivedTypes.Count > 0)
        {
            return null; // Force legacy path for polymorphic dispatch
        }

        var key = (sourceCollectionType, destCollectionType, sourceElementType, destElementType);
        return _collectionPlans.GetOrAdd(key, _ =>
        {
            var builder = new ExecutionPlanBuilder(this);
            return builder.BuildCollectionExecutionPlan(
                sourceCollectionType, destCollectionType,
                sourceElementType, destElementType,
                elementTypeMap);
        });
    }

    /// <summary>
    /// Returns all TypeMaps for iteration. Used by Mapper to find base types for Include.
    /// </summary>
    public IEnumerable<((Type, Type), TypeMap)> GetAllTypeMapsForIteration()
    {
        foreach (var kvp in _typeMaps)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }
}
