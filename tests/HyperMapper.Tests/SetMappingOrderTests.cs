using HyperMapper.Configuration;
using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests for v12.1.0 SetMappingOrder() feature - control property mapping execution order.
/// AutoMapper API compatibility: SetMappingOrder(int mappingOrder)
/// </summary>
public class SetMappingOrderTests
{
    #region Test Models

    public class Source
    {
        private int _executionCounter = 0;

        public int GetValue1()
        {
            _executionCounter += 10;
            return _executionCounter;
        }

        public int GetValue2()
        {
            _executionCounter += 5;
            return _executionCounter;
        }

        public string? First { get; set; }
        public string? Second { get; set; }
    }

    public class Destination
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public string? One { get; set; }
        public string? Two { get; set; }
    }

    public class DependentSetterDestination
    {
        private string? one;
        public string? One
        {
            get => one;
            set
            {
                one = value;
                Two = value; // Side effect: sets Two
            }
        }
        public string? Two { get; set; }
    }

    public class AccumulatingDestination
    {
        private string? id = "";
        public string? ID
        {
            get => id;
            set => id = string.Concat(ID, value); // Accumulates values
        }
    }

    public class SourceBase
    {
        public string? First { get; set; }
        public string? Second { get; set; }
    }

    public class SourceChild : SourceBase
    {
        public string? Third { get; set; }
    }

    public class DestinationForInheritance
    {
        private string? one;
        public string? One
        {
            get => one;
            set
            {
                one = value;
                Two = value; // Side effect
            }
        }
        public string? Two { get; set; }
        public string? Three { get; set; }
    }

    public class SourceWithCondition
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
    }

    public class DestinationWithCondition
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public string? ExecutionLog { get; set; } = "";
    }

    #endregion

    #region Profiles

    public class BasicOrderingProfile : Profile
    {
        public BasicOrderingProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.GetValue1());
                    opt.SetMappingOrder(2); // Maps second
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.GetValue2());
                    opt.SetMappingOrder(1); // Maps first
                });
        }
    }

    public class NullOrderProfile : Profile
    {
        public NullOrderProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.GetValue1());
                    opt.SetMappingOrder(0); // Maps second (explicit order 0)
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.GetValue2());
                    // No SetMappingOrder - null order maps first
                });
        }
    }

    public class NegativeOrderProfile : Profile
    {
        public NegativeOrderProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.GetValue1());
                    opt.SetMappingOrder(600); // Maps second
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.GetValue2());
                    opt.SetMappingOrder(-500); // Maps first
                });
        }
    }

    public class DependentSetterProfile : Profile
    {
        public DependentSetterProfile()
        {
            CreateMap<Source, DependentSetterDestination>()
                .ForMember(d => d.One, opt =>
                {
                    opt.MapFrom(s => s.First);
                    opt.SetMappingOrder(-500); // Execute first
                })
                .ForMember(d => d.Two, opt =>
                {
                    opt.MapFrom(s => s.Second);
                    opt.SetMappingOrder(600); // Execute after One, preserving independent value
                });
        }
    }

    public class DependentSetterWrongOrderProfile : Profile
    {
        public DependentSetterWrongOrderProfile()
        {
            CreateMap<Source, DependentSetterDestination>()
                .ForMember(d => d.Two, opt =>
                {
                    opt.MapFrom(s => s.Second);
                    opt.SetMappingOrder(-500); // Execute first
                })
                .ForMember(d => d.One, opt =>
                {
                    opt.MapFrom(s => s.First);
                    opt.SetMappingOrder(600); // Execute after Two
                });
        }
    }

    public class InheritanceProfile : Profile
    {
        public InheritanceProfile()
        {
            CreateMap<SourceBase, DestinationForInheritance>()
                .ForMember(d => d.One, opt =>
                {
                    opt.MapFrom(s => s.First);
                    opt.SetMappingOrder(-500); // Maps first
                })
                .ForMember(d => d.Two, opt =>
                {
                    opt.MapFrom(s => s.Second);
                    opt.SetMappingOrder(600); // Maps second
                });

            CreateMap<SourceChild, DestinationForInheritance>()
                .ForMember(d => d.One, opt =>
                {
                    opt.MapFrom(s => s.First);
                    opt.SetMappingOrder(-500); // Inherit same order from base
                })
                .ForMember(d => d.Two, opt =>
                {
                    opt.MapFrom(s => s.Second);
                    opt.SetMappingOrder(600); // Inherit same order from base
                })
                .ForMember(d => d.Three, opt => opt.MapFrom(s => s.Third));
        }
    }

    public class ConditionWithOrderProfile : Profile
    {
        public ConditionWithOrderProfile()
        {
            CreateMap<SourceWithCondition, DestinationWithCondition>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom((src, dest) =>
                    {
                        dest.ExecutionLog += "1";
                        return src.Value1;
                    });
                    opt.Condition((src, dest, val) => val > 0);
                    opt.SetMappingOrder(2); // Second
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom((src, dest) =>
                    {
                        dest.ExecutionLog += "2";
                        return src.Value2;
                    });
                    opt.SetMappingOrder(1); // First
                })
                .ForMember(d => d.Value3, opt =>
                {
                    opt.MapFrom((src, dest) =>
                    {
                        dest.ExecutionLog += "3";
                        return src.Value3;
                    });
                    opt.SetMappingOrder(3); // Third
                });
        }
    }

    public class PreConditionWithOrderProfile : Profile
    {
        public PreConditionWithOrderProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.GetValue1());
                    opt.PreCondition(s => true); // Always executes
                    opt.SetMappingOrder(2);
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.GetValue2());
                    opt.SetMappingOrder(1);
                });
        }
    }

    public class DestinationDependentProfile : Profile
    {
        public DestinationDependentProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom((src, dest) =>
                    {
                        // This should execute second, so dest.Value2 should already be set
                        return dest.Value2 + 100;
                    });
                    opt.SetMappingOrder(2);
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom((src, dest) => 50);
                    opt.SetMappingOrder(1);
                });
        }
    }

    public class ExecutionPlanOrderProfile : Profile
    {
        public ExecutionPlanOrderProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.One, opt =>
                {
                    opt.MapFrom(s => s.First);
                    opt.SetMappingOrder(-500);
                })
                .ForMember(d => d.Two, opt =>
                {
                    opt.MapFrom(s => s.Second);
                    opt.SetMappingOrder(600);
                });
        }
    }

    public class SameOrderProfile : Profile
    {
        public SameOrderProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.GetValue1());
                    opt.SetMappingOrder(100); // Same order value
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.GetValue2());
                    opt.SetMappingOrder(100); // Same order value
                });
        }
    }

    public class MixedOrderingProfile : Profile
    {
        public MixedOrderingProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.GetValue1());
                    opt.SetMappingOrder(100);
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.GetValue2());
                    opt.SetMappingOrder(-50);
                })
                .ForMember(d => d.One, opt =>
                {
                    opt.MapFrom(s => s.First);
                    // No SetMappingOrder - null order, maps first
                })
                .ForMember(d => d.Two, opt =>
                {
                    opt.MapFrom(s => s.Second);
                    opt.SetMappingOrder(0);
                });
        }
    }

    public class PropertyAccumulationProfile : Profile
    {
        public PropertyAccumulationProfile()
        {
            CreateMap<Source, AccumulatingDestination>()
                .ForMember(d => d.ID, opt =>
                {
                    opt.MapFrom(s => s.First);
                    opt.SetMappingOrder(-1000); // Map first to establish base value
                });
        }
    }

    #endregion

    #region Test 1: Basic ordering - higher numbers map after lower numbers

    [Fact]
    public void Should_Map_Properties_In_Specified_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BasicOrderingProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - Value2 (order 1) executes first (+5 = 5), then Value1 (order 2) (+10 = 15)
        Assert.Equal(15, result.Value1); // GetValue1 executed second: counter was 5, added 10
        Assert.Equal(5, result.Value2);  // GetValue2 executed first: counter was 0, added 5
    }

    #endregion

    #region Test 2: Null order maps first

    [Fact]
    public void Should_Map_Null_Order_Before_Explicit_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NullOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - Value2 (null order) executes first (+5 = 5), then Value1 (order 0) (+10 = 15)
        Assert.Equal(15, result.Value1); // GetValue1 executed second
        Assert.Equal(5, result.Value2);  // GetValue2 executed first (null order)
    }

    #endregion

    #region Test 3: Negative ordering

    [Fact]
    public void Should_Support_Negative_Mapping_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<NegativeOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - Value2 (order -500) executes first (+5 = 5), then Value1 (order 600) (+10 = 15)
        Assert.Equal(15, result.Value1); // GetValue1 executed second
        Assert.Equal(5, result.Value2);  // GetValue2 executed first (negative order)
    }

    #endregion

    #region Test 4: Dependent property setters (primary use case)

    [Fact]
    public void Should_Handle_Dependent_Property_Setters()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DependentSetterProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { First = "first", Second = "second" };

        // Act
        var result = mapper.Map<DependentSetterDestination>(source);

        // Assert
        Assert.Equal("first", result.One); // One is set correctly
        Assert.Equal("second", result.Two); // Two preserves its independent value, not the side effect from One
    }

    [Fact]
    public void Should_Handle_Dependent_Property_Setters_Wrong_Order()
    {
        // Arrange - This demonstrates the WRONG order (what happens without SetMappingOrder)
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DependentSetterWrongOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { First = "first", Second = "second" };

        // Act
        var result = mapper.Map<DependentSetterDestination>(source);

        // Assert - With wrong order, One's side effect overwrites Two
        Assert.Equal("first", result.One); // One is set correctly
        Assert.Equal("first", result.Two); // Two gets overwritten by One's setter side effect
    }

    #endregion

    #region Test 5: Mapping child type with same ordering as base

    [Fact]
    public void Should_Map_Child_Type_With_Consistent_Ordering()
    {
        // Arrange - Tests that SetMappingOrder works with inheritance hierarchies
        var config = new MapperConfiguration(cfg => cfg.AddProfile<InheritanceProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceChild { First = "first", Second = "second", Third = "third" };

        // Act - Map using derived type
        var result = mapper.Map<DestinationForInheritance>(source);

        // Assert - Dependent setter behavior works correctly with ordered members
        Assert.Equal("first", result.One);
        Assert.Equal("second", result.Two); // Preserves independent value, not side effect
        Assert.Equal("third", result.Three);
    }

    #endregion

    #region Test 6: Interaction with Condition

    [Fact]
    public void Should_Respect_Order_With_Condition()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConditionWithOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { Value1 = 10, Value2 = 20, Value3 = 30 };

        // Act
        var result = mapper.Map<DestinationWithCondition>(source);

        // Assert - Order is respected: 2, 1 (condition passes), 3
        Assert.Equal("213", result.ExecutionLog); // Execution order: Value2 (1), Value1 (2), Value3 (3)
        Assert.Equal(10, result.Value1);
        Assert.Equal(20, result.Value2);
        Assert.Equal(30, result.Value3);
    }

    #endregion

    #region Test 7: Interaction with PreCondition

    [Fact]
    public void Should_Respect_Order_With_PreCondition()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PreConditionWithOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - PreCondition evaluated before value resolution, but member positioned in sequence according to order
        Assert.Equal(15, result.Value1); // Executes second
        Assert.Equal(5, result.Value2);  // Executes first
    }

    #endregion

    #region Test 8: Interaction with destination-dependent mappings

    [Fact]
    public void Should_Order_Destination_Dependent_Mappings()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DestinationDependentProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert
        Assert.Equal(150, result.Value1); // Value2 (50) + 100 = 150 (executed second, references Value2)
        Assert.Equal(50, result.Value2);  // Executed first
    }

    #endregion

    #region Test 9: Execution plan compilation (simple mapping without conditions)

    [Fact]
    public void Should_Support_Execution_Plan_With_Mapping_Order()
    {
        // Arrange - Simple mapping without conditions/transforms should use execution plan
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExecutionPlanOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { First = "first", Second = "second" };

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - Execution plan should be generated and respect order
        Assert.Equal("first", result.One);
        Assert.Equal("second", result.Two);
    }

    #endregion

    #region Test 10: Same order value - insertion order preserved

    [Fact]
    public void Should_Preserve_Insertion_Order_For_Same_Order_Value()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SameOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - With same order value, should maintain ForMember() definition order
        // Value1 defined first, so executes first (+10 = 10)
        // Value2 defined second, so executes second (+5 = 15)
        Assert.Equal(10, result.Value1); // GetValue1 executed first
        Assert.Equal(15, result.Value2); // GetValue2 executed second
    }

    #endregion

    #region Test 11: Multiple properties with mixed ordering

    [Fact]
    public void Should_Handle_Multiple_Properties_With_Mixed_Ordering()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MixedOrderingProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { First = "first", Second = "second" };

        // Act
        var result = mapper.Map<Destination>(source);

        // Assert - Execution order: One (null) -> Value2 (-50) -> Two (0) -> Value1 (100)
        // GetValue2 executes before GetValue1
        Assert.Equal(15, result.Value1); // GetValue1 executed last (+10 after counter was at 5)
        Assert.Equal(5, result.Value2);  // GetValue2 executed second (+5)
        Assert.Equal("first", result.One);
        Assert.Equal("second", result.Two);
    }

    #endregion

    #region Test 12: Property concatenation use case

    [Fact]
    public void Should_Handle_Property_Accumulation()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PropertyAccumulationProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { First = "ABC" };

        // Act
        var result = mapper.Map<AccumulatingDestination>(source);

        // Assert
        Assert.Equal("ABC", result.ID); // Initial empty string + "ABC"
    }

    #endregion
}
