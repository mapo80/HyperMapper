using System;
using System.Collections.Generic;
using System.Linq;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// Represents a Profile class containing mapping definitions.
/// </summary>
internal sealed class ProfileInfo : IEquatable<ProfileInfo>
{
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";
    /// <summary>
    /// Full class name including parent class names for nested types (e.g., "OuterClass_InnerProfile").
    /// Used for generating unique file names.
    /// </summary>
    public string FullClassName { get; set; } = "";
    public List<MappingDefinition> Mappings { get; set; } = new();

    public bool Equals(ProfileInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ClassName == other.ClassName &&
               Namespace == other.Namespace &&
               FullClassName == other.FullClassName &&
               Mappings.Count == other.Mappings.Count &&
               Mappings.SequenceEqual(other.Mappings);
    }

    public override bool Equals(object? obj) => Equals(obj as ProfileInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (ClassName?.GetHashCode() ?? 0);
            hash = hash * 31 + (Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + (FullClassName?.GetHashCode() ?? 0);
            hash = hash * 31 + Mappings.Count;
            return hash;
        }
    }
}
