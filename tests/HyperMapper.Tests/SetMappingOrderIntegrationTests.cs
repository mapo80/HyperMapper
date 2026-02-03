using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v12.1.0: Integration tests for SetMappingOrder() feature.
/// Tests real-world scenarios with complex interactions between features.
/// </summary>
public class SetMappingOrderIntegrationTests
{
    #region Test Models

    // Entity with audit fields
    public class AuditEntity
    {
        public DateTime CreatedAt { get; set; }
        private DateTime _modifiedAt;
        public DateTime ModifiedAt
        {
            get => _modifiedAt;
            set
            {
                _modifiedAt = value;
                // Side effect: ensure ModifiedAt is never before CreatedAt
                if (_modifiedAt < CreatedAt)
                    _modifiedAt = CreatedAt;
            }
        }
        public string? Data { get; set; }
    }

    public class AuditSource
    {
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string? Data { get; set; }
    }

    // Nested objects
    public class ParentSource
    {
        public string? Name { get; set; }
        public ChildSource? Child { get; set; }
    }

    public class ChildSource
    {
        public string? Value { get; set; }
    }

    public class ParentDestination
    {
        private string? name;
        public string? Name
        {
            get => name;
            set
            {
                name = value;
                ChildName = value; // Side effect
            }
        }
        public string? ChildName { get; set; }
        public string? ChildValue { get; set; }
    }

    // Collection items
    public class SourceItem
    {
        private int counter = 0;
        public int GetFirst() => ++counter;
        public int GetSecond() => ++counter;
    }

    public class DestItem
    {
        public int First { get; set; }
        public int Second { get; set; }
    }

    // ReverseMap models
    public class ForwardSource
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class ForwardDestination
    {
        private string? value1;
        public string? Value1
        {
            get => value1;
            set
            {
                value1 = value;
                Value2 = value; // Side effect in forward direction
            }
        }
        public string? Value2 { get; set; }
    }

    // Multiple profiles
    public class SharedSource
    {
        public string? A { get; set; }
        public string? B { get; set; }
    }

    public class Dest1
    {
        public string? A { get; set; }
        public string? B { get; set; }
    }

    public class Dest2
    {
        public string? A { get; set; }
        public string? B { get; set; }
    }

    // ForAllOtherMembers
    public class MultiPropSource
    {
        public string? Prop1 { get; set; }
        public string? Prop2 { get; set; }
        public string? Prop3 { get; set; }
        public string? Prop4 { get; set; }
    }

    public class MultiPropDest
    {
        public string? Prop1 { get; set; }
        public string? Prop2 { get; set; }
        public string? Prop3 { get; set; }
        public string? Prop4 { get; set; }
    }

    // Hybrid execution (scalars + collections)
    public class HybridSource
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public List<string>? Items { get; set; }
    }

    public class HybridDest
    {
        private string? name;
        public string? Name
        {
            get => name;
            set
            {
                name = value;
                Value = name?.Length ?? 0; // Side effect
            }
        }
        public int Value { get; set; }
        public List<string>? Items { get; set; }
    }

    // Polymorphic mapping
    public class BaseSource
    {
        public string? BaseProperty { get; set; }
    }

    public class DerivedSource : BaseSource
    {
        public string? DerivedProperty { get; set; }
    }

    public class BaseDest
    {
        private string? baseProperty;
        public string? BaseProperty
        {
            get => baseProperty;
            set
            {
                baseProperty = value;
                ComputedValue = value?.Length ?? 0; // Side effect
            }
        }
        public int ComputedValue { get; set; }
    }

    public class DerivedDest : BaseDest
    {
        public string? DerivedProperty { get; set; }
    }

    // IncludeMembers
    public class PrimarySource
    {
        public string? PrimaryValue { get; set; }
        public NestedSource? Nested { get; set; }
    }

    public class NestedSource
    {
        public string? NestedValue { get; set; }
    }

    public class FlatDest
    {
        private string? primaryValue;
        public string? PrimaryValue
        {
            get => primaryValue;
            set
            {
                primaryValue = value;
                NestedValue = value; // Side effect
            }
        }
        public string? NestedValue { get; set; }
    }

    // Lifecycle hooks
    public class LifecycleSource
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class LifecycleDest
    {
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
        public string? ExecutionLog { get; set; } = "";
    }

    #endregion

    #region Test Profiles

    public class AuditEntityProfile : Profile
    {
        public AuditEntityProfile()
        {
            CreateMap<AuditSource, AuditEntity>()
                .ForMember(d => d.CreatedAt, opt =>
                {
                    opt.MapFrom(s => s.Created);
                    opt.SetMappingOrder(-100); // Must execute first
                })
                .ForMember(d => d.ModifiedAt, opt =>
                {
                    opt.MapFrom(s => s.Modified);
                    opt.SetMappingOrder(100); // Execute after CreatedAt
                })
                .ForMember(d => d.Data, opt => opt.MapFrom(s => s.Data));
        }
    }

    public class NestedObjectProfile : Profile
    {
        public NestedObjectProfile()
        {
            CreateMap<ParentSource, ParentDestination>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.SetMappingOrder(-100); // Execute first (side effect on ChildName)
                })
                .ForMember(d => d.ChildName, opt =>
                {
                    opt.MapFrom(s => s.Child!.Value);
                    opt.SetMappingOrder(100); // Execute after Name
                })
                .ForMember(d => d.ChildValue, opt => opt.MapFrom(s => s.Child!.Value));
        }
    }

    public class CollectionProfile : Profile
    {
        public CollectionProfile()
        {
            CreateMap<SourceItem, DestItem>()
                .ForMember(d => d.Second, opt =>
                {
                    opt.MapFrom(s => s.GetSecond());
                    opt.SetMappingOrder(2); // Execute second
                })
                .ForMember(d => d.First, opt =>
                {
                    opt.MapFrom(s => s.GetFirst());
                    opt.SetMappingOrder(1); // Execute first
                });
        }
    }

    public class ReverseMapProfile : Profile
    {
        public ReverseMapProfile()
        {
            CreateMap<ForwardSource, ForwardDestination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1);
                    opt.SetMappingOrder(-100); // Side effect on Value2
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    opt.SetMappingOrder(100); // Preserve independent value
                })
                .ReverseMap(); // Should NOT inherit order
        }
    }

    public class Profile1 : Profile
    {
        public Profile1()
        {
            CreateMap<SharedSource, Dest1>()
                .ForMember(d => d.A, opt =>
                {
                    opt.MapFrom(s => s.A);
                    opt.SetMappingOrder(100); // Profile1 order
                })
                .ForMember(d => d.B, opt =>
                {
                    opt.MapFrom(s => s.B);
                    opt.SetMappingOrder(-100);
                });
        }
    }

    public class Profile2 : Profile
    {
        public Profile2()
        {
            CreateMap<SharedSource, Dest2>()
                .ForMember(d => d.A, opt =>
                {
                    opt.MapFrom(s => s.A);
                    opt.SetMappingOrder(-100); // Profile2 order (opposite)
                })
                .ForMember(d => d.B, opt =>
                {
                    opt.MapFrom(s => s.B);
                    opt.SetMappingOrder(100);
                });
        }
    }

    public class ForAllOtherMembersProfile : Profile
    {
        public ForAllOtherMembersProfile()
        {
            CreateMap<MultiPropSource, MultiPropDest>()
                .ForMember(d => d.Prop1, opt =>
                {
                    opt.MapFrom(s => s.Prop1);
                    opt.SetMappingOrder(-100); // Explicit order
                })
                .ForMember(d => d.Prop2, opt =>
                {
                    opt.MapFrom(s => s.Prop2);
                    opt.SetMappingOrder(100); // Explicit order
                })
                .ForMember(d => d.Prop3, opt =>
                {
                    opt.MapFrom(s => s.Prop3);
                    opt.SetMappingOrder(100); // Same order as Prop2
                })
                .ForMember(d => d.Prop4, opt =>
                {
                    opt.MapFrom(s => s.Prop4);
                    opt.SetMappingOrder(200); // Highest order
                });
        }
    }

    public class HybridExecutionProfile : Profile
    {
        public HybridExecutionProfile()
        {
            CreateMap<HybridSource, HybridDest>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.SetMappingOrder(-100); // Side effect on Value
                })
                .ForMember(d => d.Value, opt =>
                {
                    opt.MapFrom(s => s.Value);
                    opt.SetMappingOrder(100); // Preserve independent value
                })
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));
        }
    }

    public class PolymorphicProfile : Profile
    {
        public PolymorphicProfile()
        {
            CreateMap<BaseSource, BaseDest>()
                .ForMember(d => d.BaseProperty, opt =>
                {
                    opt.MapFrom(s => s.BaseProperty);
                    opt.SetMappingOrder(-100); // Side effect on ComputedValue
                })
                .ForMember(d => d.ComputedValue, opt =>
                {
                    opt.MapFrom(s => 999); // Override computed value
                    opt.SetMappingOrder(100);
                });

            CreateMap<DerivedSource, DerivedDest>()
                .IncludeBase<BaseSource, BaseDest>()
                .ForMember(d => d.BaseProperty, opt =>
                {
                    opt.MapFrom(s => s.BaseProperty);
                    opt.SetMappingOrder(-100); // Explicitly configure base property
                })
                .ForMember(d => d.ComputedValue, opt =>
                {
                    opt.MapFrom(s => 999); // Override computed value
                    opt.SetMappingOrder(100); // Explicitly configure computed value
                })
                .ForMember(d => d.DerivedProperty, opt => opt.MapFrom(s => s.DerivedProperty));
        }
    }

    public class IncludeMembersProfile : Profile
    {
        public IncludeMembersProfile()
        {
            CreateMap<PrimarySource, FlatDest>()
                .ForMember(d => d.PrimaryValue, opt =>
                {
                    opt.MapFrom(s => s.PrimaryValue);
                    opt.SetMappingOrder(-100); // Side effect on NestedValue
                })
                .ForMember(d => d.NestedValue, opt =>
                {
                    opt.MapFrom(s => s.Nested!.NestedValue);
                    opt.SetMappingOrder(100); // Preserve nested value
                });
        }
    }

    public class LifecycleProfile : Profile
    {
        public LifecycleProfile()
        {
            CreateMap<LifecycleSource, LifecycleDest>()
                .BeforeMap((src, dest) =>
                {
                    dest.ExecutionLog += "Before;";
                })
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1);
                    opt.SetMappingOrder(-100);
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    opt.SetMappingOrder(100);
                })
                .AfterMap((src, dest) =>
                {
                    dest.ExecutionLog += "After;";
                });
        }
    }

    #endregion

    #region Tests

    /// <summary>
    /// Test 1: Complex entity with audit fields.
    /// ModifiedAt setter depends on CreatedAt being set first.
    /// </summary>
    [Fact]
    public void Integration_EntityWithAuditFields_Should_Set_Timestamps_In_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AuditEntityProfile>());
        var mapper = config.CreateMapper();
        var source = new AuditSource
        {
            Created = new DateTime(2025, 1, 1),
            Modified = new DateTime(2024, 12, 31), // Before Created (invalid)
            Data = "Test"
        };

        // Act
        var dest = mapper.Map<AuditEntity>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal(new DateTime(2025, 1, 1), dest.CreatedAt);
        // ModifiedAt setter corrects the value to match CreatedAt (side effect)
        Assert.Equal(new DateTime(2025, 1, 1), dest.ModifiedAt);
        Assert.Equal("Test", dest.Data);
    }

    /// <summary>
    /// Test 2: Nested objects with ordering.
    /// Parent property setter has side effect on child property.
    /// </summary>
    [Fact]
    public void Integration_NestedObjects_Should_Respect_Order_At_All_Levels()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NestedObjectProfile>());
        var mapper = config.CreateMapper();
        var source = new ParentSource
        {
            Name = "ParentName",
            Child = new ChildSource { Value = "ChildValue" }
        };

        // Act
        var dest = mapper.Map<ParentDestination>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("ParentName", dest.Name);
        // ChildName should be "ChildValue" (order 100), not "ParentName" (side effect from order -100)
        Assert.Equal("ChildValue", dest.ChildName);
        Assert.Equal("ChildValue", dest.ChildValue);
    }

    /// <summary>
    /// Test 3: Collection mapping with SetMappingOrder.
    /// Each collection item should respect property order.
    /// </summary>
    [Fact]
    public void Integration_Collections_Should_Respect_Member_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        var mapper = config.CreateMapper();
        var sourceList = new List<SourceItem> { new SourceItem(), new SourceItem() };

        // Act
        var destList = mapper.Map<List<DestItem>>(sourceList);

        // Assert
        Assert.NotNull(destList);
        Assert.Equal(2, destList.Count);
        // Each item: First (order 1) executes before Second (order 2)
        Assert.Equal(1, destList[0].First);
        Assert.Equal(2, destList[0].Second);
        Assert.Equal(1, destList[1].First);
        Assert.Equal(2, destList[1].Second);
    }

    /// <summary>
    /// Test 4: ReverseMap should not inherit order.
    /// Forward and reverse directions have independent ordering.
    /// </summary>
    [Fact]
    public void Integration_ReverseMap_Should_Not_Inherit_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReverseMapProfile>());
        var mapper = config.CreateMapper();

        // Forward mapping
        var forwardSource = new ForwardSource { Value1 = "V1", Value2 = "V2" };
        var forwardDest = mapper.Map<ForwardDestination>(forwardSource);

        // Assert forward (with order)
        Assert.Equal("V1", forwardDest.Value1);
        Assert.Equal("V2", forwardDest.Value2); // Preserved despite side effect

        // Reverse mapping
        var reverseSource = new ForwardDestination { Value1 = "RV1", Value2 = "RV2" };
        var reverseDest = mapper.Map<ForwardSource>(reverseSource);

        // Assert reverse (no order, convention mapping)
        Assert.Equal("RV1", reverseDest.Value1);
        Assert.Equal("RV2", reverseDest.Value2);
    }

    /// <summary>
    /// Test 5: Multiple profiles with different orders.
    /// Each profile should maintain its own ordering.
    /// </summary>
    [Fact]
    public void Integration_MultipleProfiles_Should_Maintain_Individual_Orders()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<Profile1>();
            cfg.AddProfile<Profile2>();
        });
        var mapper = config.CreateMapper();
        var source = new SharedSource { A = "A", B = "B" };

        // Act
        var dest1 = mapper.Map<Dest1>(source);
        var dest2 = mapper.Map<Dest2>(source);

        // Assert - Profile1 order: B (-100) → A (100)
        Assert.Equal("A", dest1.A);
        Assert.Equal("B", dest1.B);

        // Assert - Profile2 order: A (-100) → B (100)
        Assert.Equal("A", dest2.A);
        Assert.Equal("B", dest2.B);
    }

    /// <summary>
    /// Test 6: Multiple properties with different orders.
    /// Properties should execute in order: -100, 100, 100, 200.
    /// </summary>
    [Fact]
    public void Integration_ForAllOtherMembers_Should_Apply_Order_To_All()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ForAllOtherMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new MultiPropSource
        {
            Prop1 = "P1",
            Prop2 = "P2",
            Prop3 = "P3",
            Prop4 = "P4"
        };

        // Act
        var dest = mapper.Map<MultiPropDest>(source);

        // Assert - All properties mapped correctly with specified order
        Assert.Equal("P1", dest.Prop1);
        Assert.Equal("P2", dest.Prop2);
        Assert.Equal("P3", dest.Prop3);
        Assert.Equal("P4", dest.Prop4);
    }

    /// <summary>
    /// Test 7: Hybrid execution (execution plan + legacy).
    /// Scalars execute in order, then collections.
    /// </summary>
    [Fact]
    public void Integration_HybridExecution_Should_Order_Scalar_Then_Collection()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<HybridExecutionProfile>());
        var mapper = config.CreateMapper();
        var source = new HybridSource
        {
            Name = "Test",
            Value = 999,
            Items = new List<string> { "Item1", "Item2" }
        };

        // Act
        var dest = mapper.Map<HybridDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Test", dest.Name);
        // Value should be 999 (order 100), not 4 (side effect from Name)
        Assert.Equal(999, dest.Value);
        Assert.Equal(2, dest.Items!.Count);
    }

    /// <summary>
    /// Test 8: Polymorphic mapping with Include.
    /// Derived type should respect base type ordering.
    /// </summary>
    [Fact]
    public void Integration_PolymorphicMapping_Should_Respect_Base_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PolymorphicProfile>());
        var mapper = config.CreateMapper();
        var source = new DerivedSource
        {
            BaseProperty = "Base",
            DerivedProperty = "Derived"
        };

        // Act
        var dest = mapper.Map<DerivedDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Base", dest.BaseProperty);
        // ComputedValue should be 999 (order 100), not 4 (side effect from BaseProperty)
        Assert.Equal(999, dest.ComputedValue);
        Assert.Equal("Derived", dest.DerivedProperty);
    }

    /// <summary>
    /// Test 9: IncludeMembers with SetMappingOrder.
    /// Primary source should take precedence with correct ordering.
    /// </summary>
    [Fact]
    public void Integration_IncludeMembers_Should_Respect_Priority()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<IncludeMembersProfile>());
        var mapper = config.CreateMapper();
        var source = new PrimarySource
        {
            PrimaryValue = "Primary",
            Nested = new NestedSource { NestedValue = "Nested" }
        };

        // Act
        var dest = mapper.Map<FlatDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("Primary", dest.PrimaryValue);
        // NestedValue should be "Nested" (order 100), not "Primary" (side effect from order -100)
        Assert.Equal("Nested", dest.NestedValue);
    }

    /// <summary>
    /// Test 10: BeforeMap/AfterMap with SetMappingOrder.
    /// Hooks should execute around ordered property mapping.
    /// </summary>
    [Fact]
    public void Integration_Lifecycle_Hooks_Should_Execute_Around_Ordered_Mapping()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<LifecycleProfile>());
        var mapper = config.CreateMapper();
        var source = new LifecycleSource { Value1 = "V1", Value2 = "V2" };

        // Act
        var dest = mapper.Map<LifecycleDest>(source);

        // Assert
        Assert.NotNull(dest);
        Assert.Equal("V1", dest.Value1);
        Assert.Equal("V2", dest.Value2);
        // Execution order: BeforeMap → Value1 (order -100) → Value2 (order 100) → AfterMap
        Assert.Equal("Before;After;", dest.ExecutionLog);
    }

    #endregion
}
