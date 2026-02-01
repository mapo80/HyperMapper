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
