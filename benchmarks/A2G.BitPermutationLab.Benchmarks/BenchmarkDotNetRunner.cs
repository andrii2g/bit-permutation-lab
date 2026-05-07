using A2G.BitPermutationLab.Benchmarking;
using A2G.BitPermutationLab.Core;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace A2G.BitPermutationLab.Benchmarks;

internal static class BenchmarkDotNetRunner
{
    public static Summary Run(IReadOnlyList<BenchmarkScenario> scenarios)
    {
        CodecPipeline pipeline = new();
        ScenarioBenchmarks.CaseSource = [.. CreateCases(scenarios, pipeline)];

        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default.WithId("Default"));

        return BenchmarkDotNet.Running.BenchmarkRunner.Run<ScenarioBenchmarks>(config);
    }

    private static IEnumerable<ScenarioBenchmarkCase> CreateCases(IReadOnlyList<BenchmarkScenario> scenarios, CodecPipeline pipeline)
    {
        foreach (BenchmarkScenario scenario in scenarios)
        {
            foreach (ulong value in scenario.Values)
            {
                CodecResult encoded = pipeline.Encode(value, scenario.Parameters);
                yield return new ScenarioBenchmarkCase(
                    $"{scenario.ScenarioId}:{value}",
                    scenario.ScenarioId,
                    scenario.Name,
                    scenario.ValueRangeKind,
                    scenario.Weights.SelectionWeight,
                    scenario.Weights.ExpectedCostFactor,
                    scenario.Weights.IsRequiredBaseline,
                    value,
                    scenario.Parameters,
                    encoded);
            }
        }
    }
}
