namespace HyperMapper;

/// <summary>
/// Custom type converter interface - compatible with AutoMapper.ITypeConverter
/// </summary>
public interface ITypeConverter<in TSource, TDestination>
{
    /// <summary>
    /// Performs conversion from source to destination type.
    /// </summary>
    TDestination Convert(TSource source, TDestination destination, ResolutionContext context);
}
