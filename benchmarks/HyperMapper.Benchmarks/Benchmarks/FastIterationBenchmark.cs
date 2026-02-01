using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HyperMapper.Benchmarks.Models;
using HyperMapper.Benchmarks.Profiles;

namespace HyperMapper.Benchmarks.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1000)]
[MemoryDiagnoser]
[RankColumn]
public class FastIterationBenchmark
{
    // ===== FLATTENING SCENARIO =====
    private ModelObject _flatteningSource = null!;
    private global::HyperMapper.IMapper _flatMapper = null!;
    private global::HyperMapper.IMapper _flatMapperCodeGen = null!;

    // ===== DEEP NESTING SCENARIO (5 levels for speed) =====
    private DeepLevel1Source _deepSource = null!;
    private global::HyperMapper.IMapper _deepMapper = null!;
    private global::HyperMapper.IMapper _deepMapperCodeGen = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Flattening test data
        _flatteningSource = new ModelObject
        {
            BaseDate = DateTime.Now,
            Sub = new ModelSubObject
            {
                ProperName = "Test",
                SubSub = new ModelSubSubObject { IAmACoolProperty = "Cool" }
            },
            Sub2 = new ModelSubObject { ProperName = "Test2" },
            SubWithExtraName = new ModelSubObject { ProperName = "ExtraTest" }
        };

        // Runtime mapper for flattening
        var flatConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperFlatteningProfile>();
        });
        _flatMapper = flatConfig.CreateMapper();

        // CodeGen mapper for flattening
        var flatCodeGenConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperFlatteningProfile>();
        });
        global::HyperMapper.Generated.HyperMapperGeneratedRegistry.Initialize(flatCodeGenConfig);
        _flatMapperCodeGen = flatCodeGenConfig.CreateMapper();

        // Deep nesting test data (5 levels)
        _deepSource = new DeepLevel1Source
        {
            Id = 1,
            Name = "Root",
            Level2 = new DeepLevel2Source
            {
                Value = "L2",
                Level3 = new DeepLevel3Source
                {
                    Value = "L3",
                    Level4 = new DeepLevel4Source
                    {
                        Value = "L4",
                        Level5 = new DeepLevel5Source
                        {
                            FinalValue = "End"
                        }
                    }
                }
            }
        };

        // Runtime mapper for deep nesting
        var deepConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperDeepProfile>();
        });
        _deepMapper = deepConfig.CreateMapper();

        // CodeGen mapper for deep nesting
        var deepCodeGenConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperDeepProfile>();
        });
        global::HyperMapper.Generated.HyperMapperGeneratedRegistry.Initialize(deepCodeGenConfig);
        _deepMapperCodeGen = deepCodeGenConfig.CreateMapper();
    }

    // ===== FLATTENING BENCHMARKS =====

    [Benchmark(Baseline = true, Description = "Flat_Manual")]
    public ModelDto FlatteningManual()
    {
        return new ModelDto
        {
            BaseDate = _flatteningSource.BaseDate,
            SubProperName = _flatteningSource.Sub.ProperName,
            Sub2ProperName = _flatteningSource.Sub2.ProperName,
            SubWithExtraNameProperName = _flatteningSource.SubWithExtraName.ProperName,
            SubSubSubIAmACoolProperty = _flatteningSource.Sub.SubSub?.IAmACoolProperty ?? string.Empty
        };
    }

    [Benchmark(Description = "Flat_Runtime")]
    public ModelDto FlatteningRuntime()
    {
        return _flatMapper.Map<ModelObject, ModelDto>(_flatteningSource);
    }

    [Benchmark(Description = "Flat_CodeGen")]
    public ModelDto FlatteningCodeGen()
    {
        return _flatMapperCodeGen.Map<ModelObject, ModelDto>(_flatteningSource);
    }

    // ===== DEEP NESTING BENCHMARKS (5 levels) =====

    [Benchmark(Description = "Deep5_Manual")]
    public DeepLevel1Destination DeepNestingManual()
    {
        var l2 = _deepSource.Level2;
        var l3 = l2?.Level3;
        var l4 = l3?.Level4;
        var l5 = l4?.Level5;

        return new DeepLevel1Destination
        {
            Id = _deepSource.Id,
            Name = _deepSource.Name,
            Level2 = l2 != null ? new DeepLevel2Destination
            {
                Value = l2.Value,
                Level3 = l3 != null ? new DeepLevel3Destination
                {
                    Value = l3.Value,
                    Level4 = l4 != null ? new DeepLevel4Destination
                    {
                        Value = l4.Value,
                        Level5 = l5 != null ? new DeepLevel5Destination
                        {
                            FinalValue = l5.FinalValue
                        } : null
                    } : null
                } : null
            } : null
        };
    }

    [Benchmark(Description = "Deep5_Runtime")]
    public DeepLevel1Destination DeepNestingRuntime()
    {
        return _deepMapper.Map<DeepLevel1Source, DeepLevel1Destination>(_deepSource);
    }

    [Benchmark(Description = "Deep5_CodeGen")]
    public DeepLevel1Destination DeepNestingCodeGen()
    {
        return _deepMapperCodeGen.Map<DeepLevel1Source, DeepLevel1Destination>(_deepSource);
    }
}
