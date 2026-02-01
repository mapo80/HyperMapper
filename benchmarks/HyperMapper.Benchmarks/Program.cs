using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Columns;

namespace HyperMapper.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        IConfig config;

        if (args.Contains("--small"))
        {
            // Quick iteration mode: ~2-3 min total
            config = ManualConfig.Create(DefaultConfig.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddJob(Job.ShortRun
                    .WithWarmupCount(1)
                    .WithIterationCount(3))
                .AddColumn(RankColumn.Arabic)
                .AddColumn(StatisticColumn.Median)
                .AddExporter(MarkdownExporter.GitHub)
                .WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default
                    .WithRatioStyle(BenchmarkDotNet.Columns.RatioStyle.Percentage));
        }
        else
        {
            // Full mode: ~30-40 min, statistically accurate
            config = ManualConfig.Create(DefaultConfig.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddColumn(RankColumn.Arabic)
                .AddColumn(StatisticColumn.Median)
                .AddExporter(MarkdownExporter.GitHub)
                .WithSummaryStyle(BenchmarkDotNet.Reports.SummaryStyle.Default
                    .WithRatioStyle(BenchmarkDotNet.Columns.RatioStyle.Percentage));
        }

        // Filter out custom args before passing to BenchmarkDotNet
        var benchmarkArgs = args.Where(a => a != "--small").ToArray();

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(benchmarkArgs, config);
    }
}
