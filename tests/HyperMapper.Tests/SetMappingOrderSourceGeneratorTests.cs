using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// v12.1.0: Unit tests for SetMappingOrder() in Source Generator (CodeGen mode).
/// Tests property mapping execution order at compile-time code generation.
/// Ensures generated code respects SetMappingOrder configuration.
/// </summary>
public class SetMappingOrderSourceGeneratorTests
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
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
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

    public class MixedOrderDestination
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
    }

    public class SourceWithCondition
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public bool IsActive { get; set; }
    }

    public class DestWithCondition
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    #endregion

    #region Test Profiles

    public class CodeGenBasicOrderProfile : Profile
    {
        public CodeGenBasicOrderProfile()
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

    public class CodeGenDependentSetterProfile : Profile
    {
        public CodeGenDependentSetterProfile()
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

    public class CodeGenMixedOrderProfile : Profile
    {
        public CodeGenMixedOrderProfile()
        {
            CreateMap<Source, MixedOrderDestination>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1);
                    opt.SetMappingOrder(100); // Maps third
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    // No SetMappingOrder - null order maps first
                })
                .ForMember(d => d.Value3, opt =>
                {
                    opt.MapFrom(s => s.Value3);
                    opt.SetMappingOrder(-50); // Maps second (after null, before 100)
                });
        }
    }

    public class CodeGenOrderWithPreConditionProfile : Profile
    {
        public CodeGenOrderWithPreConditionProfile()
        {
            CreateMap<SourceWithCondition, DestWithCondition>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.PreCondition(s => s.IsActive);
                    opt.MapFrom(s => s.Value1);
                    opt.SetMappingOrder(2); // Maps second (if PreCondition passes)
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    opt.SetMappingOrder(1); // Maps first
                });
        }
    }

    public class CodeGenOrderWithConditionProfile : Profile
    {
        public CodeGenOrderWithConditionProfile()
        {
            CreateMap<SourceWithCondition, DestWithCondition>()
                .ForMember(d => d.Value1, opt =>
                {
                    opt.MapFrom(s => s.Value1);
                    opt.Condition((src, dest, val) => val > 0);
                    opt.SetMappingOrder(2); // Maps second (condition evaluated after)
                })
                .ForMember(d => d.Value2, opt =>
                {
                    opt.MapFrom(s => s.Value2);
                    opt.SetMappingOrder(1); // Maps first
                });
        }
    }

    #endregion

    #region Tests

    /// <summary>
    /// Test 1: Verify CodeGen mode respects SetMappingOrder.
    /// Properties with lower order should execute before higher order.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Respect_Mapping_Order()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenBasicOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source();

        // Act
        var dest = mapper.Map<Destination>(source);

        // Assert
        Assert.NotNull(dest);
        // Value2 (order 1) should execute first: counter = 0 + 5 = 5
        // Value1 (order 2) should execute second: counter = 5 + 10 = 15
        Assert.Equal(5, dest.Value2);  // First execution
        Assert.Equal(15, dest.Value1); // Second execution
    }

    /// <summary>
    /// Test 2: Dependent setters with CodeGen mode.
    /// When One setter has side effect on Two, order matters.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Handle_Dependent_Setters()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenDependentSetterProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { First = "FirstValue", Second = "SecondValue" };

        // Act
        var dest = mapper.Map<DependentSetterDestination>(source);

        // Assert
        Assert.NotNull(dest);
        // One (order -500) maps first, setting both One and Two to "FirstValue"
        // Two (order 600) maps second, overwriting Two with "SecondValue"
        Assert.Equal("FirstValue", dest.One);
        Assert.Equal("SecondValue", dest.Two); // Preserved independent value
    }

    /// <summary>
    /// Test 3: Mixed null and explicit ordering with CodeGen.
    /// Null order should map first, then negative, then positive.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Handle_Mixed_Ordering()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenMixedOrderProfile>());
        var mapper = config.CreateMapper();
        var source = new Source { Value1 = 100, Value2 = 200, Value3 = 300 };

        // Act
        var dest = mapper.Map<MixedOrderDestination>(source);

        // Assert
        Assert.NotNull(dest);
        // Execution order: Value2 (null) → Value3 (-50) → Value1 (100)
        Assert.Equal(100, dest.Value1);
        Assert.Equal(200, dest.Value2);
        Assert.Equal(300, dest.Value3);
        // All values mapped correctly regardless of order
    }

    /// <summary>
    /// Test 4: SetMappingOrder with PreCondition in CodeGen.
    /// PreCondition evaluated before value resolution, but member positioned by order.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Combine_Order_With_PreCondition()
    {
        // Arrange - PreCondition passes
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenOrderWithPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { Value1 = 100, Value2 = 200, IsActive = true };

        // Act
        var dest = mapper.Map<DestWithCondition>(source);

        // Assert
        Assert.NotNull(dest);
        // Value2 (order 1) executes first
        // Value1 (order 2) executes second, PreCondition passes
        Assert.Equal(200, dest.Value2);
        Assert.Equal(100, dest.Value1);
    }

    /// <summary>
    /// Test 4b: SetMappingOrder with PreCondition fails.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Skip_Member_When_PreCondition_Fails()
    {
        // Arrange - PreCondition fails
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenOrderWithPreConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { Value1 = 100, Value2 = 200, IsActive = false };

        // Act
        var dest = mapper.Map<DestWithCondition>(source);

        // Assert
        Assert.NotNull(dest);
        // Value2 (order 1) executes and maps
        // Value1 (order 2) PreCondition fails, not mapped
        Assert.Equal(200, dest.Value2);
        Assert.Equal(0, dest.Value1); // Skipped due to PreCondition
    }

    /// <summary>
    /// Test 5: SetMappingOrder with Condition in CodeGen.
    /// Post-condition evaluated after value resolution.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Combine_Order_With_Condition()
    {
        // Arrange - Condition passes
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenOrderWithConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { Value1 = 100, Value2 = 200 };

        // Act
        var dest = mapper.Map<DestWithCondition>(source);

        // Assert
        Assert.NotNull(dest);
        // Value2 (order 1) executes first
        // Value1 (order 2) executes second, condition (val > 0) passes
        Assert.Equal(200, dest.Value2);
        Assert.Equal(100, dest.Value1);
    }

    /// <summary>
    /// Test 5b: SetMappingOrder with Condition fails.
    /// </summary>
    [Fact]
    public void CodeGen_Should_Skip_Member_When_Condition_Fails()
    {
        // Arrange - Condition fails
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CodeGenOrderWithConditionProfile>());
        var mapper = config.CreateMapper();
        var source = new SourceWithCondition { Value1 = -50, Value2 = 200 }; // Value1 < 0

        // Act
        var dest = mapper.Map<DestWithCondition>(source);

        // Assert
        Assert.NotNull(dest);
        // Value2 (order 1) executes and maps
        // Value1 (order 2) resolved, but condition (val > 0) fails
        Assert.Equal(200, dest.Value2);
        Assert.Equal(0, dest.Value1); // Skipped due to Condition failure
    }

    #endregion
}
