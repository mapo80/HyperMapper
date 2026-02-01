namespace HyperMapper;

/// <summary>
/// v8.0.0: Attribute-based mapping configuration.
/// Apply to a destination type to automatically create a mapping from the specified source type.
/// AutoMapper API compatible.
/// </summary>
/// <example>
/// [AutoMap(typeof(Source))]
/// public class Dest { }
///
/// [AutoMap(typeof(Source), ReverseMap = true)]
/// public class Dest2 { }
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoMapAttribute : Attribute
{
    /// <summary>
    /// The source type to map from.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// When true, also creates a reverse mapping from destination to source.
    /// Default is false.
    /// </summary>
    public bool ReverseMap { get; set; }

    /// <summary>
    /// Creates an AutoMap attribute for mapping from the specified source type.
    /// </summary>
    /// <param name="sourceType">The source type to map from.</param>
    public AutoMapAttribute(Type sourceType)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
    }
}
