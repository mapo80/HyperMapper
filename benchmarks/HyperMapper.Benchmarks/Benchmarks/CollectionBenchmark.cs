using BenchmarkDotNet.Attributes;
using HyperMapper.Benchmarks.Models;
using HyperMapper.Benchmarks.Profiles;

namespace HyperMapper.Benchmarks.Benchmarks;

/// <summary>
/// Collection mapping benchmark - measures scalability with different sizes
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class CollectionBenchmark
{
    private List<CollectionItemSource> _smallList = null!;   // 10 items
    private List<CollectionItemSource> _mediumList = null!;  // 100 items
    private List<CollectionItemSource> _largeList = null!;   // 1000 items

    private HyperMapper.IMapper _linksMapper = null!;
    private HyperMapper.IMapper _linksMapperCodeGen = null!;
    private AutoMapper.IMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallList = CreateSourceList(10);
        _mediumList = CreateSourceList(100);
        _largeList = CreateSourceList(1000);

        // HyperMapper Runtime setup
        var linksConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperCollectionProfile>();
        });
        _linksMapper = linksConfig.CreateMapper();

        // HyperMapper CodeGen setup
        var linksCodeGenConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperCollectionProfile>();
        });
        HyperMapper.Generated.HyperMapperGeneratedRegistry.Initialize(linksCodeGenConfig);
        _linksMapperCodeGen = linksCodeGenConfig.CreateMapper();

        // AutoMapper setup
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoCollectionProfile>();
        });
        _autoMapper = autoConfig.CreateMapper();
    }

    private static List<CollectionItemSource> CreateSourceList(int count)
    {
        var list = new List<CollectionItemSource>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new CollectionItemSource
            {
                Id = i,
                Name = $"Item {i}",
                Price = i * 1.5m,
                Quantity = i + 1
            });
        }
        return list;
    }

    // Small collection (10 items)

    [Benchmark(Baseline = true)]
    public List<CollectionItemDestination> Manual_Small()
    {
        var result = new List<CollectionItemDestination>(_smallList.Count);
        foreach (var item in _smallList)
        {
            result.Add(new CollectionItemDestination
            {
                Id = item.Id,
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity
            });
        }
        return result;
    }

    [Benchmark]
    public List<CollectionItemDestination> HyperMapper_Small()
    {
        return _linksMapper.Map<List<CollectionItemDestination>>(_smallList);
    }

    [Benchmark]
    public List<CollectionItemDestination> HyperMapper_CodeGen_Small()
    {
        return _linksMapperCodeGen.Map<List<CollectionItemDestination>>(_smallList);
    }

    [Benchmark]
    public List<CollectionItemDestination> AutoMapper_Small()
    {
        return _autoMapper.Map<List<CollectionItemDestination>>(_smallList);
    }

    // Medium collection (100 items)

    [Benchmark]
    public List<CollectionItemDestination> Manual_Medium()
    {
        var result = new List<CollectionItemDestination>(_mediumList.Count);
        foreach (var item in _mediumList)
        {
            result.Add(new CollectionItemDestination
            {
                Id = item.Id,
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity
            });
        }
        return result;
    }

    [Benchmark]
    public List<CollectionItemDestination> HyperMapper_Medium()
    {
        return _linksMapper.Map<List<CollectionItemDestination>>(_mediumList);
    }

    [Benchmark]
    public List<CollectionItemDestination> HyperMapper_CodeGen_Medium()
    {
        return _linksMapperCodeGen.Map<List<CollectionItemDestination>>(_mediumList);
    }

    [Benchmark]
    public List<CollectionItemDestination> AutoMapper_Medium()
    {
        return _autoMapper.Map<List<CollectionItemDestination>>(_mediumList);
    }

    // Large collection (1000 items)

    [Benchmark]
    public List<CollectionItemDestination> Manual_Large()
    {
        var result = new List<CollectionItemDestination>(_largeList.Count);
        foreach (var item in _largeList)
        {
            result.Add(new CollectionItemDestination
            {
                Id = item.Id,
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity
            });
        }
        return result;
    }

    [Benchmark]
    public List<CollectionItemDestination> HyperMapper_Large()
    {
        return _linksMapper.Map<List<CollectionItemDestination>>(_largeList);
    }

    [Benchmark]
    public List<CollectionItemDestination> HyperMapper_CodeGen_Large()
    {
        return _linksMapperCodeGen.Map<List<CollectionItemDestination>>(_largeList);
    }

    [Benchmark]
    public List<CollectionItemDestination> AutoMapper_Large()
    {
        return _autoMapper.Map<List<CollectionItemDestination>>(_largeList);
    }
}
