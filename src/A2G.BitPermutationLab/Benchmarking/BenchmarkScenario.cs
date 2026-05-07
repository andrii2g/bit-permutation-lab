using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkScenario(
    string ScenarioId,
    string Name,
    CodecParameters Parameters,
    ValueRangeKind ValueRangeKind,
    IReadOnlyList<ulong> Values,
    ScenarioWeights Weights);
