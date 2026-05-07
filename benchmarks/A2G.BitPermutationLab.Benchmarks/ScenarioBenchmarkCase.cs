using A2G.BitPermutationLab.Benchmarking;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Benchmarks;

public sealed record ScenarioBenchmarkCase(
    string DisplayName,
    string ScenarioId,
    string ScenarioName,
    ValueRangeKind ValueRangeKind,
    double SelectionWeight,
    double ExpectedCostFactor,
    bool IsRequiredBaseline,
    ulong InputValue,
    CodecParameters Parameters,
    CodecResult Encoded);
