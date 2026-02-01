using System;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// v10.0.0: Represents a type transformation from AddTransform&lt;T&gt;().
/// Applies a transformation to all properties of type T.
/// </summary>
internal sealed class TransformDefinition : IEquatable<TransformDefinition>
{
    /// <summary>
    /// The type to transform (e.g., "string", "System.String").
    /// </summary>
    public string TargetType { get; set; } = "";

    /// <summary>
    /// The transformation lambda expression (e.g., "s?.Trim()" for strings).
    /// </summary>
    public string TransformExpression { get; set; } = "";

    public bool Equals(TransformDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TargetType == other.TargetType &&
               TransformExpression == other.TransformExpression;
    }

    public override bool Equals(object? obj) => Equals(obj as TransformDefinition);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (TargetType?.GetHashCode() ?? 0);
            hash = hash * 31 + (TransformExpression?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
