using System.Reflection;
using HyperMapper.Internal;
using Microsoft.Extensions.Logging;

namespace HyperMapper;

/// <summary>
/// Mapper configuration - compatible with AutoMapper.MapperConfiguration
/// </summary>
public class MapperConfiguration
{
    private readonly TypeMapRegistry _registry = new();
    // v8.0.0: Store typed delegates directly to avoid wrapper lambda overhead
    private readonly Dictionary<(Type, Type), Delegate> _generatedPlans = new();

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
        : this(configure, null)
    {
    }

    public MapperConfiguration(Action<IMapperConfigurationExpression> configure, ILoggerFactory? loggerFactory)
    {
        var expression = new MapperConfigurationExpression();
        configure(expression);

        foreach (var profile in expression.Profiles)
        {
            foreach (var typeMap in profile.TypeMaps)
            {
                _registry.Register(typeMap);
            }
        }

        // Finalize all configurations to pre-compute member sets
        _registry.FinalizeAll();

        // Build execution plans at configuration time for fast runtime mapping
        _registry.BuildAllExecutionPlans();
    }

    /// <summary>
    /// Validate the configuration (checks all mappings are valid).
    /// </summary>
    public void AssertConfigurationIsValid()
    {
        _registry.Validate();
    }

    /// <summary>
    /// Create a mapper instance from this configuration.
    /// </summary>
    public IMapper CreateMapper()
    {
        return new Mapper(_registry, _generatedPlans);
    }

    /// <summary>
    /// Registers a compile-time generated mapping function (v6.0.0).
    /// Called by the Source Generator's registry initializer.
    /// v8.0.0: Stores typed delegate directly without wrapper lambda for maximum performance.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDest">The destination type.</typeparam>
    /// <param name="generatedMapper">The generated mapping function.</param>
    public void RegisterGeneratedPlan<TSource, TDest>(Func<TSource?, TDest?> generatedMapper)
    {
        // v8.0.0: Store typed delegate directly - no wrapper lambda overhead
        _generatedPlans[(typeof(TSource), typeof(TDest))] = generatedMapper;
    }

    /// <summary>
    /// Gets a typed generated mapping plan if one exists (v8.0.0).
    /// Used by Mapper for fast typed lookup without boxing.
    /// </summary>
    internal Func<TSource?, TDest?>? GetGeneratedPlan<TSource, TDest>()
    {
        if (_generatedPlans.TryGetValue((typeof(TSource), typeof(TDest)), out var plan))
            return (Func<TSource?, TDest?>)plan;
        return null;
    }

    /// <summary>
    /// Gets the raw generated plans dictionary for the Mapper (v8.0.0).
    /// </summary>
    internal Dictionary<(Type, Type), Delegate> GeneratedPlans => _generatedPlans;
}

/// <summary>
/// Configuration expression interface - compatible with AutoMapper.IMapperConfigurationExpression
/// </summary>
public interface IMapperConfigurationExpression
{
    /// <summary>
    /// Add a profile of type TProfile.
    /// </summary>
    void AddProfile<TProfile>() where TProfile : Profile, new();

    /// <summary>
    /// Add a profile instance.
    /// </summary>
    void AddProfile(Profile profile);

    /// <summary>
    /// v8.0.0: Scan assembly for profiles and [AutoMap] attributed types.
    /// </summary>
    void AddMaps(Assembly assembly);

    /// <summary>
    /// v8.0.0: Scan multiple assemblies for profiles and [AutoMap] attributed types.
    /// </summary>
    void AddMaps(params Assembly[] assemblies);

    /// <summary>
    /// v8.0.0: Scan assemblies containing the specified marker types.
    /// </summary>
    void AddMaps(IEnumerable<Type> assemblyMarkerTypes);
}

internal class MapperConfigurationExpression : IMapperConfigurationExpression
{
    internal List<Profile> Profiles { get; } = new();

    public void AddProfile<TProfile>() where TProfile : Profile, new()
    {
        Profiles.Add(new TProfile());
    }

    public void AddProfile(Profile profile)
    {
        Profiles.Add(profile);
    }

    public void AddMaps(Assembly assembly)
    {
        AddMaps(new[] { assembly });
    }

    public void AddMaps(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }
    }

    public void AddMaps(IEnumerable<Type> assemblyMarkerTypes)
    {
        var assemblies = assemblyMarkerTypes
            .Select(t => t.Assembly)
            .Distinct()
            .ToArray();
        AddMaps(assemblies);
    }

    private void ScanAssembly(Assembly assembly)
    {
        var exportedTypes = assembly.GetExportedTypes();

        // v8.0.0: Discover profiles in the assembly
        var profileTypes = exportedTypes
            .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var profileType in profileTypes)
        {
            var profile = (Profile)Activator.CreateInstance(profileType)!;
            Profiles.Add(profile);
        }

        // v8.0.0: Discover [AutoMap] attributed types and create mappings
        var autoMapProfile = new AutoMapDiscoveryProfile();
        foreach (var type in exportedTypes)
        {
            var autoMapAttributes = type.GetCustomAttributes<AutoMapAttribute>();
            foreach (var attr in autoMapAttributes)
            {
                autoMapProfile.CreateMapFromAttribute(attr.SourceType, type, attr.ReverseMap);
            }
        }

        if (autoMapProfile.HasMappings)
        {
            Profiles.Add(autoMapProfile);
        }
    }
}

/// <summary>
/// v8.0.0: Internal profile for [AutoMap] discovered mappings.
/// </summary>
internal class AutoMapDiscoveryProfile : Profile
{
    private int _mappingCount;

    public bool HasMappings => _mappingCount > 0;

    internal void CreateMapFromAttribute(Type sourceType, Type destType, bool reverseMap)
    {
        // Use non-generic CreateMap
        CreateMapInternal(sourceType, destType);
        _mappingCount++;

        if (reverseMap)
        {
            CreateMapInternal(destType, sourceType);
            _mappingCount++;
        }
    }

    private void CreateMapInternal(Type sourceType, Type destType)
    {
        // Create TypeMap directly
        var typeMap = new TypeMap(sourceType, destType);
        AddTypeMap(typeMap);
    }
}
