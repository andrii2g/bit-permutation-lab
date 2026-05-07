namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkResultRow(
    string ScenarioId,
    string ScenarioName,
    ValueRangeKind ValueRangeKind,
    double SelectionWeight,
    bool IsRequiredBaseline,
    ulong InputValue,
    int OutputLength,
    double EncodeNanoseconds,
    double DecodeNanoseconds,
    double RoundTripNanoseconds,
    bool RoundTripValid,
    string SampleOutput);
