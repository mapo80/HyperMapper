using System.Globalization;
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
/// FULL benchmark - Multiple resolvers (3) to measure realistic overhead.
/// Compares: Manual vs Lambda vs Multiple Resolvers vs CodeGen vs AutoMapper
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Config(typeof(Config))]
public class ValueResolverBenchmark
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

    private ValueResolverSource _source = null!;
    private HyperMapper.IMapper _mapperWithResolver = null!;
    private HyperMapper.IMapper _mapperWithLambda = null!;
    private AutoMapper.IMapper _autoMapperResolver = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new ValueResolverSource
        {
            FirstName = "John",
            LastName = "Doe",
            Amount = 1234.56m,
            Status = "Active"
        };

        // HyperMapper with multiple resolvers
        var resolverConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperValueResolverProfile>();
        });
        _mapperWithResolver = resolverConfig.CreateMapper();

        // HyperMapper with lambda
        var lambdaConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperLambdaProfile>();
        });
        _mapperWithLambda = lambdaConfig.CreateMapper();

        // AutoMapper with resolvers
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperValueResolverProfile>();
        });
        _autoMapperResolver = autoConfig.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public ValueResolverDestination Manual()
    {
        return new ValueResolverDestination
        {
            FullName = $"{_source.FirstName} {_source.LastName}",
            FormattedAmount = _source.Amount.ToString("C2", CultureInfo.GetCultureInfo("en-US")),
            StatusEnum = ParseStatus(_source.Status)
        };
    }

    [Benchmark]
    public ValueResolverDestination HyperMapper_Lambda()
    {
        return _mapperWithLambda.Map<ValueResolverSource, ValueResolverDestination>(_source);
    }

    [Benchmark]
    public ValueResolverDestination HyperMapper_Resolver()
    {
        return _mapperWithResolver.Map<ValueResolverSource, ValueResolverDestination>(_source);
    }

    [Benchmark]
    public ValueResolverDestination HyperMapper_CodeGen()
    {
        return StaticMapper.Map<ValueResolverSource, ValueResolverDestination>(_source)!;
    }

    [Benchmark]
    public ValueResolverDestination AutoMapper_Resolver()
    {
        return _autoMapperResolver.Map<ValueResolverSource, ValueResolverDestination>(_source);
    }

    private static VRStatusEnum ParseStatus(string status) => status switch
    {
        "Active" => VRStatusEnum.Active,
        "Inactive" => VRStatusEnum.Inactive,
        "Pending" => VRStatusEnum.Pending,
        _ => VRStatusEnum.Unknown
    };
}
