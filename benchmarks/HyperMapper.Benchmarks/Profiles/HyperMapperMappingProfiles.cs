using HyperMapper.Benchmarks.Models;

namespace HyperMapper.Benchmarks.Profiles;

/// <summary>
/// HyperMapper profiles for all benchmark scenarios
/// </summary>
public class HyperMapperSimpleProfile : Profile
{
    public HyperMapperSimpleProfile()
    {
        CreateMap<SimpleSource, SimpleDestination>();
    }
}

public class HyperMapperFlatteningProfile : Profile
{
    public HyperMapperFlatteningProfile()
    {
        CreateMap<ModelObject, ModelDto>()
            .ForMember(d => d.SubProperName, o => o.MapFrom(s => s.Sub.ProperName))
            .ForMember(d => d.Sub2ProperName, o => o.MapFrom(s => s.Sub2.ProperName))
            .ForMember(d => d.SubWithExtraNameProperName, o => o.MapFrom(s => s.SubWithExtraName.ProperName))
            .ForMember(d => d.SubSubSubIAmACoolProperty, o => o.MapFrom(s => s.Sub.SubSub != null ? s.Sub.SubSub.IAmACoolProperty : string.Empty));
    }
}

public class HyperMapperCollectionProfile : Profile
{
    public HyperMapperCollectionProfile()
    {
        CreateMap<CollectionItemSource, CollectionItemDestination>();
        CreateMap<CollectionContainerSource, CollectionContainerDestination>();
    }
}

public class HyperMapperComplexProfile : Profile
{
    public HyperMapperComplexProfile()
    {
        CreateMap<ComplexAddressSource, ComplexAddressDestination>();
        CreateMap<ComplexSource, ComplexDestination>();
    }
}

public class HyperMapperDeepProfile : Profile
{
    public HyperMapperDeepProfile()
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

// ========== VALUE RESOLVER PROFILES (SMALL - single resolver) ==========

public class HyperMapperSmallResolverProfile : Profile
{
    public HyperMapperSmallResolverProfile()
    {
        CreateMap<ValueResolverSmallSource, ValueResolverSmallDestination>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<SmallFullNameResolver>());
    }
}

public class HyperMapperSmallLambdaProfile : Profile
{
    public HyperMapperSmallLambdaProfile()
    {
        CreateMap<ValueResolverSmallSource, ValueResolverSmallDestination>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
    }
}

public class SmallFullNameResolver : IValueResolver<ValueResolverSmallSource, ValueResolverSmallDestination, string>
{
    public string Resolve(ValueResolverSmallSource source, ValueResolverSmallDestination destination,
        string destMember, ResolutionContext context)
        => $"{source.FirstName} {source.LastName}";
}

// ========== VALUE RESOLVER PROFILES (FULL - multi-resolver) ==========

public class HyperMapperValueResolverProfile : Profile
{
    public HyperMapperValueResolverProfile()
    {
        CreateMap<ValueResolverSource, ValueResolverDestination>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameBenchResolver>())
            .ForMember(d => d.FormattedAmount, opt => opt.MapFrom<AmountBenchResolver>())
            .ForMember(d => d.StatusEnum, opt => opt.MapFrom<StatusBenchResolver>());
    }
}

public class HyperMapperLambdaProfile : Profile
{
    public HyperMapperLambdaProfile()
    {
        CreateMap<ValueResolverSource, ValueResolverDestination>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.FormattedAmount, opt => opt.MapFrom(s => s.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"))))
            .ForMember(d => d.StatusEnum, opt => opt.MapFrom(s =>
                s.Status == "Active" ? VRStatusEnum.Active :
                s.Status == "Inactive" ? VRStatusEnum.Inactive :
                s.Status == "Pending" ? VRStatusEnum.Pending :
                VRStatusEnum.Unknown));
    }
}

// Full Resolvers
public class FullNameBenchResolver : IValueResolver<ValueResolverSource, ValueResolverDestination, string>
{
    public string Resolve(ValueResolverSource source, ValueResolverDestination dest,
        string member, ResolutionContext context)
        => $"{source.FirstName} {source.LastName}";
}

public class AmountBenchResolver : IValueResolver<ValueResolverSource, ValueResolverDestination, string>
{
    public string Resolve(ValueResolverSource source, ValueResolverDestination dest,
        string member, ResolutionContext context)
        => source.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
}

public class StatusBenchResolver : IValueResolver<ValueResolverSource, ValueResolverDestination, VRStatusEnum>
{
    public VRStatusEnum Resolve(ValueResolverSource source, ValueResolverDestination dest,
        VRStatusEnum member, ResolutionContext context)
        => source.Status switch
        {
            "Active" => VRStatusEnum.Active,
            "Inactive" => VRStatusEnum.Inactive,
            "Pending" => VRStatusEnum.Pending,
            _ => VRStatusEnum.Unknown
        };
}
