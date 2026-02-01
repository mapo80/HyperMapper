namespace HyperMapper;

/// <summary>
/// v8.0.0: Specifies which members should be validated during configuration validation.
/// AutoMapper API compatible.
/// </summary>
public enum MemberList
{
    /// <summary>
    /// No member validation is performed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Validate that all source members have a destination.
    /// </summary>
    Source = 1,

    /// <summary>
    /// Validate that all destination members have a source (default).
    /// </summary>
    Destination = 2
}
