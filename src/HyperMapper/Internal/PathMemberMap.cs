using System.Linq.Expressions;

namespace HyperMapper.Internal;

/// <summary>
/// v8.0.0: Stores ForPath() configuration for deeply nested property paths.
/// </summary>
internal class PathMemberMap
{
    /// <summary>
    /// Path segments from root to leaf (e.g., ["Address", "Street"]).
    /// </summary>
    public List<string> PathSegments { get; }

    /// <summary>
    /// The full destination path as a string (e.g., "Address.Street").
    /// </summary>
    public string FullPath => string.Join(".", PathSegments);

    /// <summary>
    /// The source expression for mapping.
    /// </summary>
    public LambdaExpression? SourceExpression { get; set; }

    /// <summary>
    /// Compiled source value resolver.
    /// </summary>
    public Delegate? SourceValueResolver { get; set; }

    /// <summary>
    /// Whether this path should be ignored.
    /// </summary>
    public bool Ignored { get; set; }

    /// <summary>
    /// Optional condition for this path mapping.
    /// </summary>
    public Func<object, bool>? Condition { get; set; }

    public PathMemberMap(List<string> pathSegments)
    {
        PathSegments = pathSegments ?? throw new ArgumentNullException(nameof(pathSegments));
    }
}
