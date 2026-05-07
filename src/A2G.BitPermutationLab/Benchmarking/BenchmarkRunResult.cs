namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkRunResult(
    BenchmarkExecutionOptions Options,
    IReadOnlyList<BenchmarkResultRow> Rows,
    IReadOnlyList<SkippedBenchmarkRow> SkippedRows,
    DateTimeOffset TimestampUtc);
