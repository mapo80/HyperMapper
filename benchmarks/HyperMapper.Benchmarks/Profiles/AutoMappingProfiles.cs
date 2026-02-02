using HyperMapper.Benchmarks.Models;

namespace HyperMapper.Benchmarks.Profiles;

/// <summary>
/// AutoMapper profiles for all benchmark scenarios
/// </summary>
public class AutoSimpleProfile : AutoMapper.Profile
{
    public AutoSimpleProfile()
    {
        CreateMap<SimpleSource, SimpleDestination>();
    }
}

public class AutoFlatteningProfile : AutoMapper.Profile
{
    public AutoFlatteningProfile()
    {
        CreateMap<ModelObject, ModelDto>()
            .ForMember(d => d.SubProperName, o => o.MapFrom(s => s.Sub.ProperName))
            .ForMember(d => d.Sub2ProperName, o => o.MapFrom(s => s.Sub2.ProperName))
            .ForMember(d => d.SubWithExtraNameProperName, o => o.MapFrom(s => s.SubWithExtraName.ProperName))
            .ForMember(d => d.SubSubSubIAmACoolProperty, o => o.MapFrom(s => s.Sub.SubSub != null ? s.Sub.SubSub.IAmACoolProperty : string.Empty));
    }
}

public class AutoCollectionProfile : AutoMapper.Profile
{
    public AutoCollectionProfile()
    {
        CreateMap<CollectionItemSource, CollectionItemDestination>();
        CreateMap<CollectionContainerSource, CollectionContainerDestination>();
    }
}

public class AutoComplexProfile : AutoMapper.Profile
{
    public AutoComplexProfile()
    {
        CreateMap<ComplexAddressSource, ComplexAddressDestination>();
        CreateMap<ComplexSource, ComplexDestination>();
    }
}

public class AutoDeepProfile : AutoMapper.Profile
{
    public AutoDeepProfile()
    {
        CreateMap<DeepLevel10Source, DeepLevel10Destination>();
        CreateMap<DeepLevel9Source, DeepLevel9Destination>();
        CreateMap<DeepLevel8Source, DeepLevel8Destination>();
        CreateMap<DeepLevel7Source, DeepLevel7Destination>();
        CreateMap<DeepLevel6Source, DeepLevel6Destination>();
        CreateMap<DeepLevel5Source, DeepLevel5Destination>();
        CreateMap<DeepLevel4Source, DeepLevel4Destination>();
        CreateMap<DeepLevel3Source, DeepLevel3Destination>();
        CreateMap<DeepLevel2Source, DeepLevel2Destination>();
        CreateMap<DeepLevel1Source, DeepLevel1Destination>();
    }
}

// ========== VALUE RESOLVER PROFILES (SMALL - AutoMapper) ==========

public class AutoMapperSmallResolverProfile : AutoMapper.Profile
{
    public AutoMapperSmallResolverProfile()
    {
        CreateMap<ValueResolverSmallSource, ValueResolverSmallDestination>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<AutoSmallFullNameResolver>());
    }
}

public class AutoSmallFullNameResolver : AutoMapper.IValueResolver<ValueResolverSmallSource, ValueResolverSmallDestination, string>
{
    public string Resolve(ValueResolverSmallSource source, ValueResolverSmallDestination dest,
        string member, AutoMapper.ResolutionContext context)
        => $"{source.FirstName} {source.LastName}";
}

// ========== VALUE RESOLVER PROFILES (FULL - AutoMapper) ==========

public class AutoMapperValueResolverProfile : AutoMapper.Profile
{
    public AutoMapperValueResolverProfile()
    {
        CreateMap<ValueResolverSource, ValueResolverDestination>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<AutoFullNameResolver>())
            .ForMember(d => d.FormattedAmount, opt => opt.MapFrom<AutoAmountResolver>())
            .ForMember(d => d.StatusEnum, opt => opt.MapFrom<AutoStatusResolver>());
    }
}

public class AutoFullNameResolver : AutoMapper.IValueResolver<ValueResolverSource, ValueResolverDestination, string>
{
    public string Resolve(ValueResolverSource source, ValueResolverDestination dest,
        string member, AutoMapper.ResolutionContext context)
        => $"{source.FirstName} {source.LastName}";
}

public class AutoAmountResolver : AutoMapper.IValueResolver<ValueResolverSource, ValueResolverDestination, string>
{
    public string Resolve(ValueResolverSource source, ValueResolverDestination dest,
        string member, AutoMapper.ResolutionContext context)
        => source.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
}

public class AutoStatusResolver : AutoMapper.IValueResolver<ValueResolverSource, ValueResolverDestination, VRStatusEnum>
{
    public VRStatusEnum Resolve(ValueResolverSource source, ValueResolverDestination dest,
        VRStatusEnum member, AutoMapper.ResolutionContext context)
        => source.Status switch
        {
            "Active" => VRStatusEnum.Active,
            "Inactive" => VRStatusEnum.Inactive,
            "Pending" => VRStatusEnum.Pending,
            _ => VRStatusEnum.Unknown
        };
}
