namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkResultRow(
    string Profile,
    string ScenarioId,
    string ScenarioName,
    ValueRangeKind ValueRangeKind,
    double SelectionWeight,
    double ExpectedCostFactor,
    bool IsRequiredBaseline,
    int BitLength,
    MixerKind MixerKind,
    PermutationKind PermutationKind,
    int ChunkSize,
    EmitterKind EmitterKind,
    OutputKind OutputKind,
    ulong InputValue,
    int OutputLength,
    double EncodeNanoseconds,
    double DecodeNanoseconds,
    double RoundTripNanoseconds,
    bool RoundTripValid,
    string SampleOutput);
