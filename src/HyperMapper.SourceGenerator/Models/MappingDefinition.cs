using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// Represents a single CreateMap&lt;TSource, TDest&gt;() definition.
/// </summary>
internal sealed class MappingDefinition : IEquatable<MappingDefinition>
{
    public string SourceType { get; set; } = "";
    public string DestinationType { get; set; } = "";
    public string SourceTypeName { get; set; } = "";
    public string DestinationTypeName { get; set; } = "";
    public List<MemberMapping> MemberMappings { get; set; } = new();
    public bool HasConverter { get; set; }
    public string? ConverterTypeName { get; set; }

    /// <summary>
    /// v7.0.0: Lambda expression from ConvertUsing(s => ...).
    /// Only set when HasConverter is true and the converter is a simple lambda.
    /// </summary>
    public string? ConverterLambdaExpression { get; set; }

    /// <summary>
    /// v7.0.0: Whether this is an open generic mapping (e.g., Box&lt;&gt; to BoxDto&lt;&gt;).
    /// </summary>
    public bool IsOpenGeneric { get; set; }

    /// <summary>
    /// v7.0.0: Type parameter names for open generic mappings (e.g., ["T"] or ["TKey", "TValue"]).
    /// </summary>
    public List<string>? TypeParameters { get; set; }

    /// <summary>
    /// v7.0.0: Open generic source type name (e.g., "Box&lt;T&gt;").
    /// </summary>
    public string? OpenSourceType { get; set; }

    /// <summary>
    /// v7.0.0: Open generic destination type name (e.g., "BoxDto&lt;T&gt;").
    /// </summary>
    public string? OpenDestType { get; set; }

    /// <summary>
    /// v8.1.0: ValidateMemberList setting ("None", "Source", "Destination").
    /// Controls which members must be mapped or ignored.
    /// </summary>
    public string? ValidateMemberList { get; set; }

    /// <summary>
    /// v9.0.0: Constructor parameter mappings from ForCtorParam().
    /// Maps source expressions to constructor parameters.
    /// </summary>
    public List<CtorParamMapping> CtorParamMappings { get; set; } = new();

    /// <summary>
    /// v9.0.0: Whether this mapping has a custom constructor lambda from ConstructUsing().
    /// </summary>
    public bool HasCustomConstructor { get; set; }

    /// <summary>
    /// v9.0.0: The constructor lambda expression from ConstructUsing(s => new Dest(...)).
    /// </summary>
    public string? ConstructorLambdaExpression { get; set; }

    /// <summary>
    /// v9.0.0: Path mappings from ForPath() for nested property assignment.
    /// </summary>
    public List<PathMapping> PathMappings { get; set; } = new();

    /// <summary>
    /// v9.0.0: Derived type mappings from Include&lt;TDerivedSource, TDerivedDest&gt;().
    /// Used for polymorphic mapping - when base type includes derived types.
    /// </summary>
    public List<(string DerivedSource, string DerivedDest)> IncludedDerivedTypes { get; set; } = new();

    /// <summary>
    /// v9.0.0: Base type mapping from IncludeBase&lt;TBaseSource, TBaseDest&gt;().
    /// Used to inherit configuration from base type mapping.
    /// </summary>
    public (string BaseSource, string BaseDest)? IncludedBaseType { get; set; }

    /// <summary>
    /// v10.0.0: Type transformations from AddTransform&lt;T&gt;().
    /// Applies transformation to all properties of type T.
    /// </summary>
    public List<TransformDefinition> Transforms { get; set; } = new();

    /// <summary>
    /// v10.0.0: Members to flatten from source into destination via IncludeMembers().
    /// </summary>
    public List<string> IncludedMembers { get; set; } = new();

    // Roslyn symbols - not used for equality (transient)
    public ITypeSymbol? SourceTypeSymbol { get; set; }
    public ITypeSymbol? DestinationTypeSymbol { get; set; }

    public bool Equals(MappingDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SourceType == other.SourceType &&
               DestinationType == other.DestinationType &&
               HasConverter == other.HasConverter &&
               ConverterLambdaExpression == other.ConverterLambdaExpression &&
               HasCustomConstructor == other.HasCustomConstructor &&
               ConstructorLambdaExpression == other.ConstructorLambdaExpression &&
               MemberMappings.Count == other.MemberMappings.Count &&
               MemberMappings.SequenceEqual(other.MemberMappings) &&
               CtorParamMappings.Count == other.CtorParamMappings.Count &&
               CtorParamMappings.SequenceEqual(other.CtorParamMappings) &&
               PathMappings.Count == other.PathMappings.Count &&
               PathMappings.SequenceEqual(other.PathMappings);
    }

    public override bool Equals(object? obj) => Equals(obj as MappingDefinition);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (SourceType?.GetHashCode() ?? 0);
            hash = hash * 31 + (DestinationType?.GetHashCode() ?? 0);
            hash = hash * 31 + HasConverter.GetHashCode();
            hash = hash * 31 + (ConverterLambdaExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + HasCustomConstructor.GetHashCode();
            hash = hash * 31 + (ConstructorLambdaExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + MemberMappings.Count;
            hash = hash * 31 + CtorParamMappings.Count;
            hash = hash * 31 + PathMappings.Count;
            return hash;
        }
    }
}
