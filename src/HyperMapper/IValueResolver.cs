namespace HyperMapper;

/// <summary>
/// Custom value resolver interface - 100% compatible with AutoMapper.IValueResolver.
/// Allows custom resolution logic for individual member mappings.
/// </summary>
/// <typeparam name="TSource">Source type being mapped from</typeparam>
/// <typeparam name="TDestination">Destination type being mapped to</typeparam>
/// <typeparam name="TDestMember">Type of the destination member being resolved</typeparam>
public interface IValueResolver<in TSource, in TDestination, TDestMember>
{
    /// <summary>
    /// Resolves the value for the destination member.
    /// </summary>
    /// <param name="source">Source object</param>
    /// <param name="destination">Destination object (partially constructed)</param>
    /// <param name="destMember">Current value of destination member</param>
    /// <param name="context">Resolution context with access to mapper</param>
    /// <returns>Resolved value for the destination member</returns>
    TDestMember Resolve(TSource source, TDestination destination,
        TDestMember destMember, ResolutionContext context);
}
