using System;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// Simplified property information extracted from IPropertySymbol.
/// </summary>
internal sealed class PropertyInfo : IEquatable<PropertyInfo>
{
    public string Name { get; set; } = "";
    public string TypeFullName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public bool IsNullable { get; set; }
    public string? UnderlyingType { get; set; }
    public bool IsCollection { get; set; }
    public string? ElementType { get; set; }
    public bool IsEnum { get; set; }
    public bool IsSimpleType { get; set; }

    public bool Equals(PropertyInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && TypeFullName == other.TypeFullName;
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name?.GetHashCode() ?? 0) * 31 + (TypeFullName?.GetHashCode() ?? 0);
        }
    }
}
