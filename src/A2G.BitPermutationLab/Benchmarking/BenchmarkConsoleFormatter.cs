using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkConsoleFormatter
{
    public static void Write(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer, BenchmarkReportOptions report)
    {
        if (report.IncludeUnweightedReport)
        {
            WriteRawMatrix(rows, writer);
        }

        if (report.IncludeWeightedReport)
        {
            if (report.IncludeUnweightedReport)
            {
                writer.WriteLine();
            }

            WriteWeightedSummary(rows, writer);
        }
    }

    public static void WriteRawMatrix(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        IReadOnlyList<BenchmarkMatrixRow> matrixRows = BuildMatrixRows(rows);

        writer.WriteLine("Raw Performance Matrix");
        writer.WriteLine("Scenario Family | Tiny Encode ns | Tiny Decode ns | Tiny RoundTrip ns | Small Encode ns | Small Decode ns | Small RoundTrip ns | Large Encode ns | Large Decode ns | Large RoundTrip ns | Weight | Cost | Output | Alloc B/op");
        writer.WriteLine("---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---");

        foreach (BenchmarkMatrixRow row in matrixRows)
        {
            writer.WriteLine(
                $"{row.FamilyKey} | {FormatMetric(row.TinyEncodeNanoseconds)} | {FormatMetric(row.TinyDecodeNanoseconds)} | {FormatMetric(row.TinyRoundTripNanoseconds)} | {FormatMetric(row.SmallEncodeNanoseconds)} | {FormatMetric(row.SmallDecodeNanoseconds)} | {FormatMetric(row.SmallRoundTripNanoseconds)} | {FormatMetric(row.LargeEncodeNanoseconds)} | {FormatMetric(row.LargeDecodeNanoseconds)} | {FormatMetric(row.LargeRoundTripNanoseconds)} | {row.SelectionWeight:F3} | {row.ExpectedCostFactor:F2} | {row.OutputLength} | n/a");
        }
    }

    public static void WriteWeightedSummary(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        IReadOnlyList<BenchmarkWeightedSummaryRow> summaryRows = BuildWeightedSummaryRows(rows);

        writer.WriteLine("Weighting Metadata");
        writer.WriteLine("ScenarioId | Profile | Range | MinInput | MaxInput | Weight | Cost | Baseline | Bits | Mixer | Permutation | Chunk | Emitter | Output");
        writer.WriteLine("---|---|---|---:|---:|---:|---:|---|---:|---|---|---:|---|---");

        foreach (BenchmarkWeightedSummaryRow row in summaryRows)
        {
            writer.WriteLine(
                $"{row.ScenarioId} | {row.Profile} | {row.ValueRangeKind} | {row.MinInput} | {row.MaxInput} | {row.SelectionWeight:F3} | {row.ExpectedCostFactor:F2} | {row.IsRequiredBaseline} | {row.BitLength} | {row.MixerKind} | {row.PermutationKind} | {row.ChunkSize} | {row.EmitterKind} | {row.OutputKind}");
        }
    }

    private static IReadOnlyList<BenchmarkMatrixRow> BuildMatrixRows(IReadOnlyList<BenchmarkResultRow> rows)
    {
        return [.. rows
            .GroupBy(static row => GetScenarioFamilyId(row.ScenarioId), StringComparer.Ordinal)
            .Select(static group => CreateMatrixRow(group))
            .OrderByDescending(static row => row.SelectionWeight)
            .ThenBy(static row => row.FamilyKey, StringComparer.Ordinal)];
    }

    private static IReadOnlyList<BenchmarkWeightedSummaryRow> BuildWeightedSummaryRows(IReadOnlyList<BenchmarkResultRow> rows)
    {
        return [.. rows
            .GroupBy(static row => row.ScenarioId, StringComparer.Ordinal)
            .Select(static group => CreateWeightedSummaryRow(group))
            .OrderByDescending(static row => row.IsRequiredBaseline)
            .ThenByDescending(static row => row.SelectionWeight)
            .ThenBy(static row => row.ScenarioId, StringComparer.Ordinal)];
    }

    private static BenchmarkMatrixRow CreateMatrixRow(IGrouping<string, BenchmarkResultRow> group)
    {
        BenchmarkResultRow first = group.First();
        return new BenchmarkMatrixRow(
            BuildFamilyKey(first),
            GetAverage(group, ValueRangeKind.Tiny, static row => row.EncodeNanoseconds),
            GetAverage(group, ValueRangeKind.Tiny, static row => row.DecodeNanoseconds),
            GetAverage(group, ValueRangeKind.Tiny, static row => row.RoundTripNanoseconds),
            GetAverage(group, ValueRangeKind.Small, static row => row.EncodeNanoseconds),
            GetAverage(group, ValueRangeKind.Small, static row => row.DecodeNanoseconds),
            GetAverage(group, ValueRangeKind.Small, static row => row.RoundTripNanoseconds),
            GetAverage(group, ValueRangeKind.Large, static row => row.EncodeNanoseconds),
            GetAverage(group, ValueRangeKind.Large, static row => row.DecodeNanoseconds),
            GetAverage(group, ValueRangeKind.Large, static row => row.RoundTripNanoseconds),
            first.SelectionWeight,
            first.ExpectedCostFactor,
            first.OutputLength);
    }

    private static BenchmarkWeightedSummaryRow CreateWeightedSummaryRow(IGrouping<string, BenchmarkResultRow> group)
    {
        BenchmarkResultRow first = group.First();
        return new BenchmarkWeightedSummaryRow(
            first.ScenarioId,
            first.Profile,
            first.ValueRangeKind,
            group.Min(static row => row.InputValue),
            group.Max(static row => row.InputValue),
            first.SelectionWeight,
            first.ExpectedCostFactor,
            first.IsRequiredBaseline,
            first.BitLength,
            first.MixerKind,
            first.PermutationKind,
            first.ChunkSize,
            first.EmitterKind,
            first.OutputKind);
    }

    private static string BuildFamilyKey(BenchmarkResultRow row)
    {
        return $"bit{row.BitLength} + {row.MixerKind} + {row.PermutationKind} + chunk{row.ChunkSize} + {row.EmitterKind}";
    }

    private static string GetScenarioFamilyId(string scenarioId)
    {
        int separatorIndex = scenarioId.LastIndexOf(':');
        return separatorIndex >= 0 ? scenarioId[..separatorIndex] : scenarioId;
    }

    private static double? GetAverage(IEnumerable<BenchmarkResultRow> rows, ValueRangeKind range, Func<BenchmarkResultRow, double> selector)
    {
        List<double> values = [.. rows.Where(row => row.ValueRangeKind == range).Select(selector)];
        return values.Count == 0 ? null : values.Average();
    }

    private static string FormatMetric(double? value)
    {
        return value is null ? "-" : $"{value.Value:F1}";
    }

    private sealed record BenchmarkMatrixRow(
        string FamilyKey,
        double? TinyEncodeNanoseconds,
        double? TinyDecodeNanoseconds,
        double? TinyRoundTripNanoseconds,
        double? SmallEncodeNanoseconds,
        double? SmallDecodeNanoseconds,
        double? SmallRoundTripNanoseconds,
        double? LargeEncodeNanoseconds,
        double? LargeDecodeNanoseconds,
        double? LargeRoundTripNanoseconds,
        double SelectionWeight,
        double ExpectedCostFactor,
        int OutputLength);

    private sealed record BenchmarkWeightedSummaryRow(
        string ScenarioId,
        string Profile,
        ValueRangeKind ValueRangeKind,
        ulong MinInput,
        ulong MaxInput,
        double SelectionWeight,
        double ExpectedCostFactor,
        bool IsRequiredBaseline,
        int BitLength,
        MixerKind MixerKind,
        PermutationKind PermutationKind,
        int ChunkSize,
        EmitterKind EmitterKind,
        OutputKind OutputKind);
}
