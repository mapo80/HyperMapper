using System;

namespace HyperMapper.SourceGenerator.Models;

/// <summary>
/// Represents a single ForMember() configuration.
/// </summary>
internal sealed class MemberMapping : IEquatable<MemberMapping>
{
    public string DestinationMember { get; set; } = "";
    public string? SourceExpression { get; set; }
    public bool IsIgnored { get; set; }
    public bool HasPreCondition { get; set; }

    /// <summary>
    /// The PreCondition expression as a string (e.g., "source.IsActive").
    /// Only set if HasPreCondition is true and the expression was successfully extracted.
    /// </summary>
    public string? PreConditionExpression { get; set; }

    /// <summary>
    /// v8.1.0: Whether NullSubstitute() was called on this member.
    /// </summary>
    public bool HasNullSubstitute { get; set; }

    /// <summary>
    /// v8.1.0: The NullSubstitute value as a string (e.g., "\"N/A\"", "-1", "0.0m").
    /// Only set if HasNullSubstitute is true.
    /// </summary>
    public string? NullSubstituteExpression { get; set; }

    /// <summary>
    /// v8.1.0: Whether Condition() was called on this member.
    /// </summary>
    public bool HasCondition { get; set; }

    /// <summary>
    /// v8.1.0: The Condition expression as a string (e.g., "value > 0").
    /// Only set if HasCondition is true.
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// v10.0.0: Whether MapFrom has a destination parameter (src, dest) => ...
    /// </summary>
    public bool HasDestinationParameter { get; set; }

    public bool Equals(MemberMapping? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return DestinationMember == other.DestinationMember &&
               SourceExpression == other.SourceExpression &&
               IsIgnored == other.IsIgnored &&
               HasPreCondition == other.HasPreCondition &&
               PreConditionExpression == other.PreConditionExpression &&
               HasNullSubstitute == other.HasNullSubstitute &&
               NullSubstituteExpression == other.NullSubstituteExpression &&
               HasCondition == other.HasCondition &&
               ConditionExpression == other.ConditionExpression;
    }

    public override bool Equals(object? obj) => Equals(obj as MemberMapping);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (DestinationMember?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + IsIgnored.GetHashCode();
            hash = hash * 31 + HasPreCondition.GetHashCode();
            hash = hash * 31 + (PreConditionExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + HasNullSubstitute.GetHashCode();
            hash = hash * 31 + (NullSubstituteExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + HasCondition.GetHashCode();
            hash = hash * 31 + (ConditionExpression?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
