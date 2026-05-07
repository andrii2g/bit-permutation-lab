namespace A2G.BitPermutationLab.Benchmarking;

public sealed record SkippedBenchmarkRow(
    string ScenarioName,
    string? ScenarioId,
    ValueRangeKind? ValueRangeKind,
    ulong? InputValue,
    string Reason);
