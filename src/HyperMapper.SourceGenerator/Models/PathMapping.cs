using System;
using System.Collections.Generic;
using System.Linq;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// v9.0.0: Represents a single ForPath() configuration.
/// Maps a source expression to a nested destination path.
/// </summary>
internal sealed class PathMapping : IEquatable<PathMapping>
{
    /// <summary>
    /// The path segments from destination (e.g., ["Address", "Street"] for d.Address.Street).
    /// </summary>
    public List<string> PathSegments { get; set; } = new();

    /// <summary>
    /// The source expression (e.g., "source.StreetName").
    /// </summary>
    public string? SourceExpression { get; set; }

    /// <summary>
    /// Whether this path mapping is ignored.
    /// </summary>
    public bool IsIgnored { get; set; }

    public bool Equals(PathMapping? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return PathSegments.SequenceEqual(other.PathSegments) &&
               SourceExpression == other.SourceExpression &&
               IsIgnored == other.IsIgnored;
    }

    public override bool Equals(object? obj) => Equals(obj as PathMapping);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var segment in PathSegments)
            {
                hash = hash * 31 + (segment?.GetHashCode() ?? 0);
            }
            hash = hash * 31 + (SourceExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + IsIgnored.GetHashCode();
            return hash;
        }
    }
}
