using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v12.1.0: Cross-feature compatibility tests for SetMappingOrder().
/// Ensures SetMappingOrder works correctly with all existing HyperMapper features.
/// </summary>
public class SetMappingOrderCrossFeatureTests
{
    #region Test Models

    // NullSubstitute tests
    public class SourceWithNull
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class DestWithDefaults
    {
        private string? value1;
        public string? Value1
        {
            get => value1;
            set
            {
                value1 = value;
                Value2 = value; // Side effect
            }
        }
        public string? Value2 { get; set; }
    }

    // IValueResolver tests
    public class SourceForResolver
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class DestForResolver
    {
        private string? fullName;
        public string? FullName
        {
            get => fullName;
            set
            {
                fullName = value;
                NameLength = value?.Length ?? 0; // Side effect
            }
        }
        public int NameLength { get; set; }
    }

    public class FullNameResolver : IValueResolver<SourceForResolver, DestForResolver, string>
    {
        public string? Resolve(SourceForResolver source, DestForResolver destination, string? destMember, ResolutionContext context)
        {
            return $"{source.FirstName} {source.LastName}";
        }
    }

    // MaxDepth tests
    public class NodeSource
    {
        public string? Value { get; set; }
        public NodeSource? Child { get; set; }
    }

    public class NodeDest
    {
        public string? Value { get; set; }
        public NodeDest? Child { get; set; }
    }

    // PreserveReferences tests
    public class CircularSource
    {
        public string? Value { get; set; }
        public CircularSource? Reference { get; set; }
    }

    public class CircularDest
    {
        public string? Value { get; set; }
        public CircularDest? Reference { get; set; }
    }

    // AddTransform tests
    public class SourceWithStrings
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class DestWithStrings
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    // ConstructUsing tests
    public class SourceForConstructor
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class DestWithConstructor
    {
        public string? Name { get; }
        private int age;
        public int Age
        {
            get => age;
            set
            {
                age = value;
                IsAdult = value >= 18; // Side effect
            }
        }
        public bool IsAdult { get; set; }

        public DestWithConstructor(string? name)
        {
            Name = name;
        }
    }

    // ForCtorParam tests
    public class SourceForCtorParam
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class DestWithCtorParam
    {
        public string? Value1 { get; }
        public string? Value2 { get; }

        public DestWithCtorParam(string? value1, string? value2)
        {
            Value1 = value1;
            Value2 = value2;
        }
    }

    // ForPath tests
    public class FlatSource
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class NestedDest
    {
        public NestedChild? Child { get; set; }
    }

    public class NestedChild
    {
        private string? value1;
        public string? Value1
        {
            get => value1;
            set
            {
                value1 = value;
                Value2 = value; // Side effect
            }
        }
        public string? Value2 { get; set; }
    }

    #endregion

    #region Test Profiles

    public class NullSubstituteProfile : Profile
    {
        public NullSubstituteProfile()
        {
            CreateMap<SourceWithNull, DestWithDefaults>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1);
                    opt.NullSubstitute("Default1");
                    opt.SetMappingOrder(-100); // Execute first
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    opt.NullSubstitute("Default2");
                    opt.SetMappingOrder(100); // Execute after Value1
                });
        }
    }

    public class ValueResolverProfile : Profile
    {
        public ValueResolverProfile()
        {
            CreateMap<SourceForResolver, DestForResolver>()
                .ForMember(d => d.FullName, opt =>
                {
                    opt.MapFrom<FullNameResolver>();
                    opt.SetMappingOrder(-100); // Execute first (side effect on NameLength)
                })
                .ForMember(d => d.NameLength, opt =>
                {
                    opt.MapFrom(s => 999); // Override computed value
                    opt.SetMappingOrder(100); // Execute after FullName
                });
        }
    }

    public class MaxDepthProfile : Profile
    {
        public MaxDepthProfile()
        {
            CreateMap<NodeSource, NodeDest>()
                .MaxDepth(2)
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.SetMappingOrder(-100);
                })
                .ForMember(d => d.Child, opt =>
                {
                    opt.MapFrom(s => s.Child);
                    opt.SetMappingOrder(100);
                });
        }
    }

    public class PreserveReferencesProfile : Profile
    {
        public PreserveReferencesProfile()
        {
            CreateMap<CircularSource, CircularDest>()
                .PreserveReferences()
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.SetMappingOrder(-100);
                })
                .ForMember(d => d.Reference, opt =>
                {
                    opt.MapFrom(s => s.Reference);
                    opt.SetMappingOrder(100);
                });
        }
    }

    public class TransformProfile : Profile
    {
        public TransformProfile()
        {
            CreateMap<SourceWithStrings, DestWithStrings>()
                .AddTransform<string>(s => s != null ? s.Trim() : s)
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1);
                    opt.SetMappingOrder(-100);
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    opt.SetMappingOrder(100);
                });
        }
    }

    public class ConstructUsingProfile : Profile
    {
        public ConstructUsingProfile()
        {
            CreateMap<SourceForConstructor, DestWithConstructor>()
                .ConstructUsing(s => new DestWithConstructor(s.Name))
                .ForMember(d => d.Age, opt =>
                {
                    opt.MapFrom(s => s.Age);
                    opt.SetMappingOrder(100); // After constructor
                });
        }
    }

    public class ForCtorParamProfile : Profile
    {
        public ForCtorParamProfile()
        {
            CreateMap<SourceForCtorParam, DestWithCtorParam>()
                .ForCtorParam("value1", opt => opt.MapFrom(s => s.Value1))
                .ForCtorParam("value2", opt => opt.MapFrom(s => s.Value2));
        }
    }

    public class ForPathProfile : Profile
    {
        public ForPathProfile()
        {
            CreateMap<FlatSource, NestedDest>()
                .ForPath(d => d.Child!.Value1, opt => opt.MapFrom(s => s.Value1))
                .ForPath(d => d.Child!.Value2, opt => opt.MapFrom(s => s.Value2));
        }
    }

    public class ExtremeOrderProfile : Profile
    {
        public ExtremeOrderProfile()
        {
            CreateMap<SourceWithNull, DestWithDefaults>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1 ?? "Min");
                    opt.SetMappingOrder(int.MinValue); // Extreme low
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2 ?? "Max");
                    opt.SetMappingOrder(int.MaxValue); // Extreme high
                });
        }
    }

    #endregion

    #region Tests

    /// <summary>
    /// Test: SetMappingOrder with NullSubstitute.
    /// Null values should be substituted, order respected.
    /// </summary>
    [Fact]
    public void CrossFeature_NullSubstitute_Should_Work_With_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullSubstituteProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNull { Value1 = null, Value2 = null };

        // Act
        var dest = mapper.Map<DestWithDefaults>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Default1", dest.Value1);
        // Value2 should be "Default2", not "Default1" (side effect from Value1)
        Assert.Equal("Default2", dest.Value2);
    }

    /// <summary>
    /// Test: SetMappingOrder with IValueResolver.
    /// Value resolver should execute in correct order.
    /// </summary>
    [Fact]
    public void CrossFeature_ValueResolver_Should_Work_With_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ValueResolverProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForResolver { FirstName = "John", LastName = "Doe" };

        // Act
        var dest = mapper.Map<DestForResolver>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John Doe", dest.FullName);
        // NameLength should be 999 (order 100), not 8 (side effect from FullName)
        Assert.Equal(999, dest.NameLength);
    }

    /// <summary>
    /// Test: SetMappingOrder with MaxDepth.
    /// Order should be respected at each depth level.
    /// </summary>
    [Fact]
    public void CrossFeature_MaxDepth_Should_Work_With_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaxDepthProfile>());
        var mapper = config.CreateMapper();
        var source = new NodeSource
        {
            Value = "Root",
            Child = new NodeSource
            {
                Value = "Child",
                Child = new NodeSource { Value = "Grandchild" }
            }
        };

        // Act
        var dest = mapper.Map<NodeDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Root", dest.Value);
        Assert.NotNull(dest.Child);
        Assert.Equal("Child", dest.Child.Value);
        // MaxDepth = 2, so grandchild should be null
        Assert.Null(dest.Child.Child);
    }

    /// <summary>
    /// Test: SetMappingOrder with PreserveReferences.
    /// Circular references should be preserved, order respected.
    /// </summary>
    [Fact]
    public void CrossFeature_PreserveReferences_Should_Work_With_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreserveReferencesProfile>());
        var mapper = config.CreateMapper();
        var source = new CircularSource { Value = "Node1" };
        source.Reference = source; // Circular reference

        // Act
        var dest = mapper.Map<CircularDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Node1", dest.Value);
        Assert.Same(dest, dest.Reference); // Circular reference preserved
    }

    /// <summary>
    /// Test: SetMappingOrder with AddTransform.
    /// Transforms should apply after ordered mapping.
    /// </summary>
    [Fact]
    public void CrossFeature_Transform_Should_Apply_After_Ordered_Mapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TransformProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithStrings { Value1 = "  Test1  ", Value2 = "  Test2  " };

        // Act
        var dest = mapper.Map<DestWithStrings>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Test1", dest.Value1); // Trimmed
        Assert.Equal("Test2", dest.Value2); // Trimmed
    }

    /// <summary>
    /// Test: SetMappingOrder with ConstructUsing.
    /// Constructor should execute before ordered property mapping.
    /// </summary>
    [Fact]
    public void CrossFeature_ConstructUsing_Should_Execute_Before_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConstructUsingProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForConstructor { Name = "John", Age = 25 };

        // Act
        var dest = mapper.Map<DestWithConstructor>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("John", dest.Name); // Set by constructor
        Assert.Equal(25, dest.Age); // Set by ordered mapping
        Assert.True(dest.IsAdult); // Side effect from Age setter
    }

    /// <summary>
    /// Test: ForCtorParam compatibility.
    /// Constructor parameters work correctly (SetMappingOrder not applicable to ForCtorParam).
    /// </summary>
    [Fact]
    public void CrossFeature_ForCtorParam_Should_Work()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForCtorParamProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceForCtorParam { Value1 = "V1", Value2 = "V2" };

        // Act
        var dest = mapper.Map<DestWithCtorParam>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("V1", dest.Value1);
        Assert.Equal("V2", dest.Value2);
    }

    /// <summary>
    /// Test: ForPath compatibility.
    /// ForPath works correctly (SetMappingOrder not directly applicable to ForPath).
    /// </summary>
    [Fact]
    public void CrossFeature_ForPath_Should_Work()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForPathProfile>());
        var mapper = config.CreateMapper();
        var source = new FlatSource { Value1 = "V1", Value2 = "V2" };

        // Act
        var dest = mapper.Map<NestedDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.NotNull(dest.Child);
        Assert.Equal("V1", dest.Child.Value1);
        Assert.Equal("V2", dest.Child.Value2);
    }

    /// <summary>
    /// Test: SetMappingOrder with extreme order values.
    /// Int.MinValue and Int.MaxValue should work correctly.
    /// </summary>
    [Fact]
    public void CrossFeature_Extreme_Order_Values_Should_Work()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtremeOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithNull { Value1 = null, Value2 = null };

        // Act
        var dest = mapper.Map<DestWithDefaults>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Min", dest.Value1);
        // Value2 should be "Max", not "Min" (side effect)
        Assert.Equal("Max", dest.Value2);
    }

    #endregion
}
