using System.Reflection;
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
/// Simple flat object mapping benchmark - measures base overhead
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[Config(typeof(Config))]
public class SimpleMappingBenchmark
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
                .FirstOrDefault(b => b.Descriptor.WorkloadMethod.Name == "AutoMapper");

            if (autoMapperCase == null || benchmarkCase.Descriptor.WorkloadMethod.Name == "AutoMapper")
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

    private SimpleSource _source = null!;
    private HyperMapper.IMapper _linksMapper = null!;
    private AutoMapper.IMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new SimpleSource
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        // HyperMapper Runtime setup (without Source Generator)
        var linksConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperSimpleProfile>();
        });
        _linksMapper = linksConfig.CreateMapper();

        // AutoMapper setup
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoSimpleProfile>();
        });
        _autoMapper = autoConfig.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public SimpleDestination Manual()
    {
        return new SimpleDestination
        {
            Id = _source.Id,
            Name = _source.Name,
            Email = _source.Email,
            CreatedAt = _source.CreatedAt,
            IsActive = _source.IsActive
        };
    }

    /// <summary>
    /// CodeGen: Static API - maximum performance (~17ns)
    /// Uses compile-time generated code via Source Generator.
    /// </summary>
    [Benchmark]
    public SimpleDestination HyperMapper_CodeGen()
    {
        return StaticMapper.Map<SimpleSource, SimpleDestination>(_source)!;
    }

    /// <summary>
    /// Runtime: IMapper interface (~100ns)
    /// Compatible with AutoMapper API, uses reflection-based mapping.
    /// </summary>
    [Benchmark]
    public SimpleDestination HyperMapper_Runtime()
    {
        return _linksMapper.Map<SimpleSource, SimpleDestination>(_source);
    }

    [Benchmark]
    public SimpleDestination AutoMapper()
    {
        return _autoMapper.Map<SimpleSource, SimpleDestination>(_source);
    }
}
