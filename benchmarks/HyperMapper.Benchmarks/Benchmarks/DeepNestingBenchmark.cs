using BenchmarkDotNet.Attributes;
using HyperMapper.Benchmarks.Models;
using HyperMapper.Benchmarks.Profiles;

namespace HyperMapper.Benchmarks.Benchmarks;

/// <summary>
/// Deep nesting benchmark - 10 levels of nested objects
/// Measures recursive mapping performance
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class DeepNestingBenchmark
{
    private DeepLevel1Source _source = null!;

    private HyperMapper.IMapper _linksMapper = null!;
    private HyperMapper.IMapper _linksMapperCodeGen = null!;
    private AutoMapper.IMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new DeepLevel1Source
        {
            Id = 1,
            Name = "Root",
            Level2 = new DeepLevel2Source
            {
                Value = "Level2",
                Level3 = new DeepLevel3Source
                {
                    Value = "Level3",
                    Level4 = new DeepLevel4Source
                    {
                        Value = "Level4",
                        Level5 = new DeepLevel5Source
                        {
                            Value = "Level5",
                            Level6 = new DeepLevel6Source
                            {
                                Value = "Level6",
                                Level7 = new DeepLevel7Source
                                {
                                    Value = "Level7",
                                    Level8 = new DeepLevel8Source
                                    {
                                        Value = "Level8",
                                        Level9 = new DeepLevel9Source
                                        {
                                            Value = "Level9",
                                            Level10 = new DeepLevel10Source
                                            {
                                                FinalValue = "DeepestValue"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // HyperMapper Runtime setup
        var linksConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperDeepProfile>();
        });
        _linksMapper = linksConfig.CreateMapper();

        // HyperMapper CodeGen setup
        var linksCodeGenConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperDeepProfile>();
        });
        HyperMapper.Generated.HyperMapperGeneratedRegistry.Initialize(linksCodeGenConfig);
        _linksMapperCodeGen = linksCodeGenConfig.CreateMapper();

        // AutoMapper setup
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoDeepProfile>();
        });
        _autoMapper = autoConfig.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public DeepLevel1Destination Manual()
    {
        var l2 = _source.Level2;
        var l3 = l2?.Level3;
        var l4 = l3?.Level4;
        var l5 = l4?.Level5;
        var l6 = l5?.Level6;
        var l7 = l6?.Level7;
        var l8 = l7?.Level8;
        var l9 = l8?.Level9;
        var l10 = l9?.Level10;

        return new DeepLevel1Destination
        {
            Id = _source.Id,
            Name = _source.Name,
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
                            Value = l5.Value,
                            Level6 = l6 != null ? new DeepLevel6Destination
                            {
                                Value = l6.Value,
                                Level7 = l7 != null ? new DeepLevel7Destination
                                {
                                    Value = l7.Value,
                                    Level8 = l8 != null ? new DeepLevel8Destination
                                    {
                                        Value = l8.Value,
                                        Level9 = l9 != null ? new DeepLevel9Destination
                                        {
                                            Value = l9.Value,
                                            Level10 = l10 != null ? new DeepLevel10Destination
                                            {
                                                FinalValue = l10.FinalValue
                                            } : null
                                        } : null
                                    } : null
                                } : null
                            } : null
                        } : null
                    } : null
                } : null
            } : null
        };
    }

    [Benchmark]
    public DeepLevel1Destination HyperMapper()
    {
        return _linksMapper.Map<DeepLevel1Destination>(_source);
    }

    [Benchmark]
    public DeepLevel1Destination HyperMapper_CodeGen()
    {
        return _linksMapperCodeGen.Map<DeepLevel1Destination>(_source);
    }

    [Benchmark]
    public DeepLevel1Destination AutoMapper()
    {
        return _autoMapper.Map<DeepLevel1Destination>(_source);
    }
}
