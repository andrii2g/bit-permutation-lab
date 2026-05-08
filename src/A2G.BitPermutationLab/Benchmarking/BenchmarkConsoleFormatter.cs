using A2G.BitPermutationLab.Chunking;
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
                $"{row.FamilyKey} | {FormatMetric(row.TinyEncodeNanoseconds)} | {FormatMetric(row.TinyDecodeNanoseconds)} | {FormatMetric(row.TinyRoundTripNanoseconds)} | {FormatMetric(row.SmallEncodeNanoseconds)} | {FormatMetric(row.SmallDecodeNanoseconds)} | {FormatMetric(row.SmallRoundTripNanoseconds)} | {FormatMetric(row.LargeEncodeNanoseconds)} | {FormatMetric(row.LargeDecodeNanoseconds)} | {FormatMetric(row.LargeRoundTripNanoseconds)} | {row.SelectionWeight:F3} | {row.ExpectedCostFactor:F2} | {row.OutputLength} | {row.AllocatedBytes}");
        }
    }

    public static void WriteWeightedSummary(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        IReadOnlyList<BenchmarkWeightedSummaryRow> summaryRows = BuildWeightedSummaryRows(rows);

        writer.WriteLine("Weighting Metadata");
        writer.WriteLine("ScenarioId | Profile | Range | Tier | SaltSeed | MinInput | MaxInput | AlgorithmW | TierW | RangeW | EmitterW | CustomW | Weight | Cost | Baseline | Bits | Mixer | Mix Params | Permutation | Permute Params | Chunking | Chunk | Emitter | Alphabet | Output | CustomMutation | Position");
        writer.WriteLine("---|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---|---|---|---|---|---:|---|---|---|---|---");

        foreach (BenchmarkWeightedSummaryRow row in summaryRows)
        {
            writer.WriteLine(
                $"{row.ScenarioId} | {row.Profile} | {row.ValueRangeKind} | {row.ParameterTier} | {row.SaltSeed} | {row.MinInput} | {row.MaxInput} | {row.AlgorithmWeight:F3} | {row.ParameterTierWeight:F3} | {row.ValueRangeWeight:F3} | {row.EmitterWeight:F3} | {row.CustomMutationWeight:F3} | {row.SelectionWeight:F3} | {row.ExpectedCostFactor:F2} | {row.IsRequiredBaseline} | {row.BitLength} | {row.MixerKind} | {row.MixerParameters} | {row.PermutationKind} | {row.PermutationParameters} | {row.ChunkingKind} | {row.ChunkSize} | {row.EmitterKind} | {row.AlphabetKind} | {row.OutputKind} | {row.CustomMutationKind} | {row.CustomMutationPosition}");
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
            first.OutputLength,
            first.AllocatedBytes);
    }

    private static BenchmarkWeightedSummaryRow CreateWeightedSummaryRow(IGrouping<string, BenchmarkResultRow> group)
    {
        BenchmarkResultRow first = group.First();
        return new BenchmarkWeightedSummaryRow(
            first.ScenarioId,
            first.Profile,
            first.ValueRangeKind,
            first.ParameterTier,
            first.SaltSeed,
            group.Min(static row => row.InputValue),
            group.Max(static row => row.InputValue),
            first.AlgorithmWeight,
            first.ParameterTierWeight,
            first.ValueRangeWeight,
            first.EmitterWeight,
            first.CustomMutationWeight,
            first.SelectionWeight,
            first.ExpectedCostFactor,
            first.IsRequiredBaseline,
            first.BitLength,
            first.MixerKind,
            first.MixerParameters,
            first.PermutationKind,
            first.PermutationParameters,
            first.ChunkingKind,
            first.ChunkSize,
            first.EmitterKind,
            first.AlphabetKind,
            first.OutputKind,
            first.CustomMutationKind,
            first.CustomMutationPosition);
    }

    private static string BuildFamilyKey(BenchmarkResultRow row)
    {
        return $"{row.BinaryKind}({row.BitOrderKind},{row.ByteOrderKind}) + {row.MixerKind}({row.MixerParameters}) + {row.PermutationKind}({row.PermutationParameters}) + {row.ChunkingKind}(chunk{row.ChunkSize}) + {row.EmitterKind}({row.AlphabetKind},{row.OutputKind})";
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
        int OutputLength,
        long AllocatedBytes);

    private sealed record BenchmarkWeightedSummaryRow(
        string ScenarioId,
        string Profile,
        ValueRangeKind ValueRangeKind,
        ParameterTierKind ParameterTier,
        ulong SaltSeed,
        ulong MinInput,
        ulong MaxInput,
        double AlgorithmWeight,
        double ParameterTierWeight,
        double ValueRangeWeight,
        double EmitterWeight,
        double CustomMutationWeight,
        double SelectionWeight,
        double ExpectedCostFactor,
        bool IsRequiredBaseline,
        int BitLength,
        MixerKind MixerKind,
        string MixerParameters,
        PermutationKind PermutationKind,
        string PermutationParameters,
        ChunkerKind ChunkingKind,
        int ChunkSize,
        EmitterKind EmitterKind,
        AlphabetKind AlphabetKind,
        OutputKind OutputKind,
        string CustomMutationKind,
        string CustomMutationPosition);
}
