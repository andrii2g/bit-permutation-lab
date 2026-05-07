namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkConsoleFormatter
{
    public static void Write(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer, BenchmarkReportOptions report)
    {
        if (report.IncludeUnweightedReport)
        {
            WriteRaw(rows, writer);
        }

        if (report.IncludeWeightedReport)
        {
            if (report.IncludeUnweightedReport)
            {
                writer.WriteLine();
            }

            WriteWeighted(rows, writer);
        }
    }

    public static void WriteRaw(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        writer.WriteLine("Raw Performance");
        writer.WriteLine("Profile | Scenario | Range | Value | Output | Encode ns | Decode ns | Total ns | Valid");
        writer.WriteLine("---|---|---|---:|---:|---:|---:|---:|---");

        foreach (BenchmarkResultRow row in rows)
        {
            writer.WriteLine(
                $"{row.Profile} | {row.ScenarioName} | {row.ValueRangeKind} | {row.InputValue} | {row.OutputLength} | {row.EncodeNanoseconds:F1} | {row.DecodeNanoseconds:F1} | {row.RoundTripNanoseconds:F1} | {row.RoundTripValid}");
        }
    }

    public static void WriteWeighted(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        writer.WriteLine("Weighting Metadata");
        writer.WriteLine("ScenarioId | Profile | Range | Weight | Cost | Baseline | Bits | Mixer | Permutation | Chunk | Emitter | Output");
        writer.WriteLine("---|---|---|---:|---:|---|---:|---|---|---:|---|---");

        foreach (BenchmarkResultRow row in rows)
        {
            writer.WriteLine(
                $"{row.ScenarioId} | {row.Profile} | {row.ValueRangeKind} | {row.SelectionWeight:F3} | {row.ExpectedCostFactor:F2} | {row.IsRequiredBaseline} | {row.BitLength} | {row.MixerKind} | {row.PermutationKind} | {row.ChunkSize} | {row.EmitterKind} | {row.OutputKind}");
        }
    }
}
