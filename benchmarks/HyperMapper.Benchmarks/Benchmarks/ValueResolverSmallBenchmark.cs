using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using HyperMapper.Benchmarks.Models;
using HyperMapper.Benchmarks.Profiles;
using StaticMapper = HyperMapper.Generated.Mapper;

namespace HyperMapper.Benchmarks.Benchmarks;

/// <summary>
/// SMALL benchmark - Single resolver to measure base overhead.
/// Compares: Manual vs Lambda vs Single Resolver vs CodeGen vs AutoMapper
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Config(typeof(Config))]
public class ValueResolverSmallBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(new VsManualColumn());
            AddColumn(new VsAutoMapperColumn());
        }
    }

    private class VsManualColumn : IColumn
    {
        public string Id => "VsManual";
        public string ColumnName => "x Manual";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Times slower than Manual";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var manualCase = summary.BenchmarksCases
                .FirstOrDefault(b => b.Descriptor.WorkloadMethod.Name == "Manual");

            if (manualCase == null || benchmarkCase.Descriptor.WorkloadMethod.Name == "Manual")
                return "1.00x";

            var currentMean = summary[benchmarkCase]?.ResultStatistics?.Mean;
            var manualMean = summary[manualCase]?.ResultStatistics?.Mean;

            if (currentMean == null || manualMean == null)
                return "N/A";

            var ratio = currentMean.Value / manualMean.Value;
            return $"{ratio:0.00}x";
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            => GetValue(summary, benchmarkCase);

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
    }

    private class VsAutoMapperColumn : IColumn
    {
        public string Id => "VsAutoMapper";
        public string ColumnName => "x AutoMapper";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 1;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Times faster than AutoMapper (>1 = faster)";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var autoMapperCase = summary.BenchmarksCases
                .FirstOrDefault(b => b.Descriptor.WorkloadMethod.Name == "AutoMapper_Resolver");

            if (autoMapperCase == null || benchmarkCase.Descriptor.WorkloadMethod.Name == "AutoMapper_Resolver")
                return "1.00x";

            var currentMean = summary[benchmarkCase]?.ResultStatistics?.Mean;
            var autoMapperMean = summary[autoMapperCase]?.ResultStatistics?.Mean;

            if (currentMean == null || autoMapperMean == null)
                return "N/A";

            var ratio = autoMapperMean.Value / currentMean.Value;
            return $"{ratio:0.00}x";
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            => GetValue(summary, benchmarkCase);

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
    }

    private ValueResolverSmallSource _source = null!;
    private HyperMapper.IMapper _mapperWithResolver = null!;
    private HyperMapper.IMapper _mapperWithLambda = null!;
    private AutoMapper.IMapper _autoMapperResolver = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new ValueResolverSmallSource
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // HyperMapper with single resolver
        var resolverConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperSmallResolverProfile>();
        });
        _mapperWithResolver = resolverConfig.CreateMapper();

        // HyperMapper with lambda
        var lambdaConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperSmallLambdaProfile>();
        });
        _mapperWithLambda = lambdaConfig.CreateMapper();

        // AutoMapper with resolver
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperSmallResolverProfile>();
        });
        _autoMapperResolver = autoConfig.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public ValueResolverSmallDestination Manual()
    {
        return new ValueResolverSmallDestination
        {
            FullName = $"{_source.FirstName} {_source.LastName}"
        };
    }

    [Benchmark]
    public ValueResolverSmallDestination HyperMapper_Lambda()
    {
        return _mapperWithLambda.Map<ValueResolverSmallSource, ValueResolverSmallDestination>(_source);
    }

    [Benchmark]
    public ValueResolverSmallDestination HyperMapper_Resolver()
    {
        return _mapperWithResolver.Map<ValueResolverSmallSource, ValueResolverSmallDestination>(_source);
    }

    [Benchmark]
    public ValueResolverSmallDestination HyperMapper_CodeGen()
    {
        return StaticMapper.Map<ValueResolverSmallSource, ValueResolverSmallDestination>(_source)!;
    }

    [Benchmark]
    public ValueResolverSmallDestination AutoMapper_Resolver()
    {
        return _autoMapperResolver.Map<ValueResolverSmallSource, ValueResolverSmallDestination>(_source);
    }
}
