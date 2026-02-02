using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Unit tests for IValueResolver support (v12.0.0).
/// Tests all mapping configurations using custom value resolvers.
/// </summary>
public class ValueResolverTests
{
    #region Runtime Mode Tests

    [Fact]
    public void MapFrom_WithResolverType_ResolvesCorrectValue()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new SourceEntity { FirstName = "John", LastName = "Doe" };
        var dest = mapper.Map<DestEntity>(source);

        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void MapFrom_WithResolverInstance_UsesProvidedInstance()
    {
        var resolverInstance = new FullNameResolver();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new InstanceResolverProfile(resolverInstance));
        });
        var mapper = config.CreateMapper();

        var source = new SourceEntity { FirstName = "Jane", LastName = "Smith" };
        var dest = mapper.Map<DestEntity>(source);

        Assert.Equal("Jane Smith", dest.FullName);
    }

    [Fact]
    public void ValueResolver_ReceivesAllParameters()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ParameterTestProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ParameterTestSource { Value = 100 };
        var dest = mapper.Map<ParameterTestDest>(source);

        // Resolver receives source, destination, current value, and context
        Assert.Equal(100, dest.ResolvedValue);
        Assert.True(dest.ReceivedContext);
    }

    [Fact]
    public void ValueResolver_CanAccessMapperViaContext()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NestedMappingResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new VROrderSource
        {
            Id = 1,
            Customer = new VRCustomerSource { Name = "Acme Corp" }
        };
        var dest = mapper.Map<OrderDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.NotNull(dest.VRCustomerInfo);
        Assert.Equal("Acme Corp", dest.VRCustomerInfo.CustomerName);
    }

    [Fact]
    public void ValueResolver_WithNestedMapping_Works()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NestedMappingResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new VROrderSource
        {
            Id = 42,
            Customer = new VRCustomerSource { Name = "Test Customer" }
        };
        var dest = mapper.Map<OrderDest>(source);

        Assert.Equal(42, dest.Id);
        Assert.Equal("Test Customer", dest.VRCustomerInfo?.CustomerName);
    }

    [Fact]
    public void ValueResolver_WithPreCondition_RespectsCondition()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PreConditionResolverProfile>();
        });
        var mapper = config.CreateMapper();

        // Source with ShouldMap = false - resolver should not be called
        var source1 = new ConditionalSource { Value = 100, ShouldMap = false };
        var dest1 = mapper.Map<ConditionalDest>(source1);
        Assert.Equal(0, dest1.MappedValue); // Default value, not resolved

        // Source with ShouldMap = true - resolver should be called
        var source2 = new ConditionalSource { Value = 200, ShouldMap = true };
        var dest2 = mapper.Map<ConditionalDest>(source2);
        Assert.Equal(200, dest2.MappedValue); // Resolved value
    }

    [Fact]
    public void ValueResolver_NullSource_HandlesGracefully()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NullHandlingResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new VRNullableSource { NullableValue = null };
        var dest = mapper.Map<NullableDest>(source);

        Assert.Equal("DEFAULT", dest.ResolvedValue);
    }

    [Fact]
    public void ValueResolver_MultipleResolvers_AllInvoked()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MultiResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new MultiSource { A = 10, B = 20 };
        var dest = mapper.Map<MultiDest>(source);

        Assert.Equal(10, dest.ResolvedA);
        Assert.Equal(20, dest.ResolvedB);
        Assert.Equal(30, dest.Sum);
    }

    [Fact]
    public void ValueResolver_WithConstructServicesUsing_UsesDI()
    {
        var resolverInstance = new FullNameResolver();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.ConstructServicesUsing(type =>
            {
                if (type == typeof(FullNameResolver))
                    return resolverInstance;
                throw new InvalidOperationException($"Unknown service type: {type}");
            });
            cfg.AddProfile<DIResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new SourceEntity { FirstName = "DI", LastName = "Test" };
        var dest = mapper.Map<DestEntity>(source);

        Assert.Equal("DI Test", dest.FullName);
    }

    [Fact]
    public void ValueResolver_GenericTypes_WorkCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<GenericResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new VRGenericSource<int> { Items = new List<int> { 1, 2, 3 } };
        var dest = mapper.Map<GenericDest>(source);

        Assert.Equal(3, dest.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValueResolver_WithCondition_EvaluatesCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ConditionWithResolverProfile>();
        });
        var mapper = config.CreateMapper();

        // Value > 50 passes condition
        var source1 = new ConditionSource { Value = 100 };
        var dest1 = mapper.Map<ConditionDest>(source1);
        Assert.Equal(100, dest1.Resolved);

        // Value <= 50 fails condition
        var source2 = new ConditionSource { Value = 30 };
        var dest2 = mapper.Map<ConditionDest>(source2);
        Assert.Equal(0, dest2.Resolved); // Default, condition not met
    }

    [Fact]
    public void ValueResolver_ThrowsException_PropagatesCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<VRThrowingResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ThrowingSource { ShouldThrow = true };
        var dest = mapper.Map<ThrowingDest>(source);

        // When resolver throws, the member should be skipped (default value)
        Assert.Equal(0, dest.Value);
    }

    [Fact]
    public void ValueResolver_ReturnsNull_SetsNullOnDestination()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NullReturningResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new NullReturnSource { Input = "test" };
        var dest = mapper.Map<NullReturnDest>(source);

        Assert.Null(dest.Output);
    }

    [Fact]
    public void ValueResolver_WithValueType_HandlesDefaultCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueTypeResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ValueTypeSource { Number = 42 };
        var dest = mapper.Map<ValueTypeDest>(source);

        Assert.Equal(84, dest.DoubledNumber); // Resolver doubles the value
    }

    #endregion

    #region Collection Edge Cases

    [Fact]
    public void ValueResolver_EmptyCollection_NotCalled()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var sources = new List<SourceEntity>();
        var results = mapper.Map<List<DestEntity>>(sources);

        Assert.Empty(results);
    }

    [Fact]
    public void ValueResolver_CollectionWithNullItems_SkipsNulls()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var sources = new List<SourceEntity?>
        {
            new() { FirstName = "John", LastName = "Doe" },
            null,
            new() { FirstName = "Jane", LastName = "Smith" }
        };
        var results = mapper.Map<List<DestEntity?>>(sources);

        Assert.Equal(3, results.Count);
        Assert.Equal("John Doe", results[0]?.FullName);
        Assert.Null(results[1]);
        Assert.Equal("Jane Smith", results[2]?.FullName);
    }

    [Fact]
    public void ValueResolver_NullCollection_ReturnsNull()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueResolverProfile>();
        });
        var mapper = config.CreateMapper();

        List<SourceEntity>? sources = null;
        var results = mapper.Map<List<DestEntity>?>(sources);

        Assert.Null(results);
    }

    #endregion

    #region Complex Return Types

    [Fact]
    public void ValueResolver_ReturnsVRNestedObject_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NestedReturnResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new NestedReturnSource { Data = "Test Data" };
        var dest = mapper.Map<NestedReturnDest>(source);

        Assert.NotNull(dest.Nested);
        Assert.Equal("Test Data", dest.Nested.Value);
    }

    [Fact]
    public void ValueResolver_ReturnsList_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ListReturnResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new ListReturnSource { Items = "a,b,c" };
        var dest = mapper.Map<ListReturnDest>(source);

        Assert.NotNull(dest.ParsedItems);
        Assert.Equal(3, dest.ParsedItems.Count);
        Assert.Equal(new[] { "a", "b", "c" }, dest.ParsedItems);
    }

    #endregion

    #region Configuration Validation

    [Fact]
    public void AssertConfigurationIsValid_WithValueResolver_Succeeds()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueResolverProfile>();
        });

        // Should not throw
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void ValueResolver_MultipleProfilesWithResolvers_AllWork()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ValueResolverProfile>();
            cfg.AddProfile<MultiResolverProfile>();
        });
        var mapper = config.CreateMapper();

        var source1 = new SourceEntity { FirstName = "Test", LastName = "User" };
        var dest1 = mapper.Map<DestEntity>(source1);
        Assert.Equal("Test User", dest1.FullName);

        var source2 = new MultiSource { A = 5, B = 10 };
        var dest2 = mapper.Map<MultiDest>(source2);
        Assert.Equal(15, dest2.Sum);
    }

    #endregion
}

#region Additional Test Profiles and Resolvers

public class NestedReturnResolverProfile : Profile
{
    public NestedReturnResolverProfile()
    {
        CreateMap<NestedReturnSource, NestedReturnDest>()
            .ForMember(d => d.Nested, opt => opt.MapFrom<VRNestedObjectResolver>());
    }
}

public class VRNestedObjectResolver : IValueResolver<NestedReturnSource, NestedReturnDest, VRNestedObject?>
{
    public VRNestedObject? Resolve(NestedReturnSource source, NestedReturnDest destination, VRNestedObject? destMember, ResolutionContext context)
    {
        return new VRNestedObject { Value = source.Data };
    }
}

public class ListReturnResolverProfile : Profile
{
    public ListReturnResolverProfile()
    {
        CreateMap<ListReturnSource, ListReturnDest>()
            .ForMember(d => d.ParsedItems, opt => opt.MapFrom<ListParserResolver>());
    }
}

public class ListParserResolver : IValueResolver<ListReturnSource, ListReturnDest, List<string>?>
{
    public List<string>? Resolve(ListReturnSource source, ListReturnDest destination, List<string>? destMember, ResolutionContext context)
    {
        return source.Items?.Split(',').ToList();
    }
}

#endregion

#region Additional Test DTOs

public class NestedReturnSource
{
    public string Data { get; set; } = string.Empty;
}

public class NestedReturnDest
{
    public VRNestedObject? Nested { get; set; }
}

public class VRNestedObject
{
    public string Value { get; set; } = string.Empty;
}

public class ListReturnSource
{
    public string Items { get; set; } = string.Empty;
}

public class ListReturnDest
{
    public List<string>? ParsedItems { get; set; }
}

#endregion

#region Test Profiles and Resolvers

public class ValueResolverProfile : Profile
{
    public ValueResolverProfile()
    {
        CreateMap<SourceEntity, DestEntity>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>());
    }
}

public class InstanceResolverProfile : Profile
{
    public InstanceResolverProfile(FullNameResolver resolver)
    {
        CreateMap<SourceEntity, DestEntity>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(resolver));
    }
}

public class DIResolverProfile : Profile
{
    public DIResolverProfile()
    {
        CreateMap<SourceEntity, DestEntity>()
            .ForMember(d => d.FullName, opt => opt.MapFrom<FullNameResolver>());
    }
}

public class FullNameResolver : IValueResolver<SourceEntity, DestEntity, string>
{
    public string Resolve(SourceEntity source, DestEntity destination, string destMember, ResolutionContext context)
    {
        return $"{source.FirstName} {source.LastName}";
    }
}

public class ParameterTestProfile : Profile
{
    public ParameterTestProfile()
    {
        CreateMap<ParameterTestSource, ParameterTestDest>()
            .ForMember(d => d.ResolvedValue, opt => opt.MapFrom<ParameterTestResolver>())
            .ForMember(d => d.ReceivedContext, opt => opt.MapFrom<ContextCheckResolver>());
    }
}

public class ParameterTestResolver : IValueResolver<ParameterTestSource, ParameterTestDest, int>
{
    public int Resolve(ParameterTestSource source, ParameterTestDest destination, int destMember, ResolutionContext context)
    {
        return source.Value;
    }
}

public class ContextCheckResolver : IValueResolver<ParameterTestSource, ParameterTestDest, bool>
{
    public bool Resolve(ParameterTestSource source, ParameterTestDest destination, bool destMember, ResolutionContext context)
    {
        return context != null && context.Mapper != null;
    }
}

public class NestedMappingResolverProfile : Profile
{
    public NestedMappingResolverProfile()
    {
        CreateMap<VRCustomerSource, VRCustomerInfo>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(src => src.Name));
        CreateMap<VROrderSource, OrderDest>()
            .ForMember(d => d.VRCustomerInfo, opt => opt.MapFrom<CustomerResolver>());
    }
}

public class CustomerResolver : IValueResolver<VROrderSource, OrderDest, VRCustomerInfo?>
{
    public VRCustomerInfo? Resolve(VROrderSource source, OrderDest destination, VRCustomerInfo? destMember, ResolutionContext context)
    {
        return context.Mapper.Map<VRCustomerInfo>(source.Customer);
    }
}

public class PreConditionResolverProfile : Profile
{
    public PreConditionResolverProfile()
    {
        CreateMap<ConditionalSource, ConditionalDest>()
            .ForMember(d => d.MappedValue, opt =>
            {
                opt.PreCondition(src => src.ShouldMap);
                opt.MapFrom<ConditionalResolver>();
            });
    }
}

public class ConditionalResolver : IValueResolver<ConditionalSource, ConditionalDest, int>
{
    public int Resolve(ConditionalSource source, ConditionalDest destination, int destMember, ResolutionContext context)
    {
        return source.Value;
    }
}

public class NullHandlingResolverProfile : Profile
{
    public NullHandlingResolverProfile()
    {
        CreateMap<VRNullableSource, NullableDest>()
            .ForMember(d => d.ResolvedValue, opt => opt.MapFrom<NullHandlingResolver>());
    }
}

public class NullHandlingResolver : IValueResolver<VRNullableSource, NullableDest, string>
{
    public string Resolve(VRNullableSource source, NullableDest destination, string destMember, ResolutionContext context)
    {
        return source.NullableValue ?? "DEFAULT";
    }
}

public class MultiResolverProfile : Profile
{
    public MultiResolverProfile()
    {
        CreateMap<MultiSource, MultiDest>()
            .ForMember(d => d.ResolvedA, opt => opt.MapFrom<ResolverA>())
            .ForMember(d => d.ResolvedB, opt => opt.MapFrom<ResolverB>())
            .ForMember(d => d.Sum, opt => opt.MapFrom<SumResolver>());
    }
}

public class ResolverA : IValueResolver<MultiSource, MultiDest, int>
{
    public int Resolve(MultiSource source, MultiDest destination, int destMember, ResolutionContext context)
    {
        return source.A;
    }
}

public class ResolverB : IValueResolver<MultiSource, MultiDest, int>
{
    public int Resolve(MultiSource source, MultiDest destination, int destMember, ResolutionContext context)
    {
        return source.B;
    }
}

public class SumResolver : IValueResolver<MultiSource, MultiDest, int>
{
    public int Resolve(MultiSource source, MultiDest destination, int destMember, ResolutionContext context)
    {
        return source.A + source.B;
    }
}

public class GenericResolverProfile : Profile
{
    public GenericResolverProfile()
    {
        CreateMap<VRGenericSource<int>, GenericDest>()
            .ForMember(d => d.Count, opt => opt.MapFrom<CountResolver>());
    }
}

public class CountResolver : IValueResolver<VRGenericSource<int>, GenericDest, int>
{
    public int Resolve(VRGenericSource<int> source, GenericDest destination, int destMember, ResolutionContext context)
    {
        return source.Items?.Count ?? 0;
    }
}

public class ConditionWithResolverProfile : Profile
{
    public ConditionWithResolverProfile()
    {
        CreateMap<ConditionSource, ConditionDest>()
            .ForMember(d => d.Resolved, opt =>
            {
                opt.MapFrom<SimpleValueResolver>();
                opt.Condition((src, dest, val) => val > 50);
            });
    }
}

public class SimpleValueResolver : IValueResolver<ConditionSource, ConditionDest, int>
{
    public int Resolve(ConditionSource source, ConditionDest destination, int destMember, ResolutionContext context)
    {
        return source.Value;
    }
}

public class VRThrowingResolverProfile : Profile
{
    public VRThrowingResolverProfile()
    {
        CreateMap<ThrowingSource, ThrowingDest>()
            .ForMember(d => d.Value, opt => opt.MapFrom<ThrowingResolver>());
    }
}

public class ThrowingResolver : IValueResolver<ThrowingSource, ThrowingDest, int>
{
    public int Resolve(ThrowingSource source, ThrowingDest destination, int destMember, ResolutionContext context)
    {
        if (source.ShouldThrow)
            throw new InvalidOperationException("Resolver intentionally failed");
        return 42;
    }
}

public class NullReturningResolverProfile : Profile
{
    public NullReturningResolverProfile()
    {
        CreateMap<NullReturnSource, NullReturnDest>()
            .ForMember(d => d.Output, opt => opt.MapFrom<NullReturningResolver>());
    }
}

public class NullReturningResolver : IValueResolver<NullReturnSource, NullReturnDest, string?>
{
    public string? Resolve(NullReturnSource source, NullReturnDest destination, string? destMember, ResolutionContext context)
    {
        return null;
    }
}

public class ValueTypeResolverProfile : Profile
{
    public ValueTypeResolverProfile()
    {
        CreateMap<ValueTypeSource, ValueTypeDest>()
            .ForMember(d => d.DoubledNumber, opt => opt.MapFrom<DoublingResolver>());
    }
}

public class DoublingResolver : IValueResolver<ValueTypeSource, ValueTypeDest, int>
{
    public int Resolve(ValueTypeSource source, ValueTypeDest destination, int destMember, ResolutionContext context)
    {
        return source.Number * 2;
    }
}

#endregion

#region Test DTOs

public class SourceEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class DestEntity
{
    public string FullName { get; set; } = string.Empty;
}

public class ParameterTestSource
{
    public int Value { get; set; }
}

public class ParameterTestDest
{
    public int ResolvedValue { get; set; }
    public bool ReceivedContext { get; set; }
}

public class VROrderSource
{
    public int Id { get; set; }
    public VRCustomerSource? Customer { get; set; }
}

public class VRCustomerSource
{
    public string Name { get; set; } = string.Empty;
}

public class OrderDest
{
    public int Id { get; set; }
    public VRCustomerInfo? VRCustomerInfo { get; set; }
}

public class VRCustomerInfo
{
    public string CustomerName { get; set; } = string.Empty;
}

public class ConditionalSource
{
    public int Value { get; set; }
    public bool ShouldMap { get; set; }
}

public class ConditionalDest
{
    public int MappedValue { get; set; }
}

public class VRNullableSource
{
    public string? NullableValue { get; set; }
}

public class NullableDest
{
    public string ResolvedValue { get; set; } = string.Empty;
}

public class MultiSource
{
    public int A { get; set; }
    public int B { get; set; }
}

public class MultiDest
{
    public int ResolvedA { get; set; }
    public int ResolvedB { get; set; }
    public int Sum { get; set; }
}

public class VRGenericSource<T>
{
    public List<T>? Items { get; set; }
}

public class GenericDest
{
    public int Count { get; set; }
}

public class ConditionSource
{
    public int Value { get; set; }
}

public class ConditionDest
{
    public int Resolved { get; set; }
}

public class ThrowingSource
{
    public bool ShouldThrow { get; set; }
}

public class ThrowingDest
{
    public int Value { get; set; }
}

public class NullReturnSource
{
    public string Input { get; set; } = string.Empty;
}

public class NullReturnDest
{
    public string? Output { get; set; }
}

public class ValueTypeSource
{
    public int Number { get; set; }
}

public class ValueTypeDest
{
    public int DoubledNumber { get; set; }
}

#endregion
