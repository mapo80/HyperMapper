using BenchmarkDotNet.Attributes;
using HyperMapper.Benchmarks.Models;
using HyperMapper.Benchmarks.Profiles;

namespace HyperMapper.Benchmarks.Benchmarks;

/// <summary>
/// Flattening benchmark - nested source to flat destination
/// Based on AutoMapper's original benchmark
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class FlatteningBenchmark
{
    private ModelObject _source = null!;
    private HyperMapper.IMapper _linksMapper = null!;
    private HyperMapper.IMapper _linksMapperCodeGen = null!;
    private AutoMapper.IMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new ModelObject
        {
            BaseDate = DateTime.Now,
            Sub = new ModelSubObject
            {
                ProperName = "Test",
                SubSub = new ModelSubSubObject { IAmACoolProperty = "Cool" }
            },
            Sub2 = new ModelSubObject { ProperName = "Test2" },
            SubWithExtraName = new ModelSubObject { ProperName = "Test3" }
        };

        // HyperMapper Runtime setup
        var linksConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperFlatteningProfile>();
        });
        _linksMapper = linksConfig.CreateMapper();

        // HyperMapper CodeGen setup
        var linksCodeGenConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperFlatteningProfile>();
        });
        HyperMapper.Generated.HyperMapperGeneratedRegistry.Initialize(linksCodeGenConfig);
        _linksMapperCodeGen = linksCodeGenConfig.CreateMapper();

        // AutoMapper setup
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoFlatteningProfile>();
        });
        _autoMapper = autoConfig.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public ModelDto Manual()
    {
        return new ModelDto
        {
            BaseDate = _source.BaseDate,
            SubProperName = _source.Sub.ProperName,
            Sub2ProperName = _source.Sub2.ProperName,
            SubWithExtraNameProperName = _source.SubWithExtraName.ProperName,
            SubSubSubIAmACoolProperty = _source.Sub.SubSub?.IAmACoolProperty ?? string.Empty
        };
    }

    [Benchmark]
    public ModelDto HyperMapper()
    {
        return _linksMapper.Map<ModelDto>(_source);
    }

    [Benchmark]
    public ModelDto HyperMapper_CodeGen()
    {
        return _linksMapperCodeGen.Map<ModelDto>(_source);
    }

    [Benchmark]
    public ModelDto AutoMapper()
    {
        return _autoMapper.Map<ModelDto>(_source);
    }
}
