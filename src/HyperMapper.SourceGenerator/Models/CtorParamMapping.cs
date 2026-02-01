using System;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// v9.0.0: Represents a single ForCtorParam() configuration.
/// Maps a source expression to a constructor parameter.
/// </summary>
internal sealed class CtorParamMapping : IEquatable<CtorParamMapping>
{
    /// <summary>
    /// The constructor parameter name (e.g., "id", "name").
    /// </summary>
    public string ParameterName { get; set; } = "";

    /// <summary>
    /// The source expression (e.g., "source.PersonId", "source.FullName").
    /// </summary>
    public string? SourceExpression { get; set; }

    public bool Equals(CtorParamMapping? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ParameterName == other.ParameterName &&
               SourceExpression == other.SourceExpression;
    }

    public override bool Equals(object? obj) => Equals(obj as CtorParamMapping);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (ParameterName?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceExpression?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
