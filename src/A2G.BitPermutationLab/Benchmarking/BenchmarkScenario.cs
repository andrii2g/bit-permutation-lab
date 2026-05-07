using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkScenario(
    string Name,
    CodecParameters Parameters,
    IReadOnlyList<ulong> Values);
