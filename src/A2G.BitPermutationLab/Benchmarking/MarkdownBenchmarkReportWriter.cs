using System.Runtime.InteropServices;

namespace A2G.BitPermutationLab.Benchmarking;

public static class MarkdownBenchmarkReportWriter
{
    public static void Write(BenchmarkRunResult result, TextWriter writer, int top = 5)
    {
        writer.WriteLine("# Bit Permutation Lab Benchmark Report");
        writer.WriteLine();
        writer.WriteLine("## Environment");
        writer.WriteLine($"- OS: {RuntimeInformation.OSDescription}");
        writer.WriteLine($"- CPU: {RuntimeInformation.ProcessArchitecture}");
        writer.WriteLine($"- Runtime: {RuntimeInformation.FrameworkDescription}");
#if DEBUG
        writer.WriteLine("- Build configuration: Debug");
#else
        writer.WriteLine("- Build configuration: Release");
#endif
        writer.WriteLine($"- Timestamp: {result.TimestampUtc:O}");
        writer.WriteLine();
        writer.WriteLine("## Parameters");
        writer.WriteLine($"- Profile: {result.Options.ProfileLabel}");
        writer.WriteLine($"- Mode: {result.Options.ModeLabel}");
        writer.WriteLine($"- Iterations: {result.Options.Iterations}");
        writer.WriteLine($"- Scenario count: {result.Rows.Select(static row => row.ScenarioId).Distinct(StringComparer.Ordinal).Count()}");
        writer.WriteLine($"- Skipped scenario count: {result.SkippedRows.Count}");
        writer.WriteLine($"- Value ranges: {string.Join(", ", result.Rows.Select(static row => row.ValueRangeKind).Distinct())}");
        writer.WriteLine();
        writer.WriteLine("## Top Results");
        WriteTopResult(writer, "Fastest encode", result.Rows.OrderBy(static row => row.EncodeNanoseconds).FirstOrDefault(), static row => $"{row.ScenarioId} ({row.EncodeNanoseconds:F1} ns)");
        WriteTopResult(writer, "Fastest decode", result.Rows.OrderBy(static row => row.DecodeNanoseconds).FirstOrDefault(), static row => $"{row.ScenarioId} ({row.DecodeNanoseconds:F1} ns)");
        WriteTopResult(writer, "Fastest round-trip", result.Rows.OrderBy(static row => row.RoundTripNanoseconds).FirstOrDefault(), static row => $"{row.ScenarioId} ({row.RoundTripNanoseconds:F1} ns)");
        WriteTopResult(writer, "Lowest allocation", result.Rows.OrderBy(static row => row.AllocatedBytes).FirstOrDefault(), static row => $"{row.ScenarioId} ({row.AllocatedBytes} B)");
        WriteTopResult(writer, "Shortest output", result.Rows.OrderBy(static row => row.OutputLength).FirstOrDefault(), static row => $"{row.ScenarioId} ({row.OutputLength})");
        writer.WriteLine();
        writer.WriteLine("## Matrix Results");
        BenchmarkConsoleFormatter.Write(writer: writer, rows: result.Rows, report: result.Options.Report);
        writer.WriteLine();
        writer.WriteLine("## Skipped Scenarios");
        if (result.SkippedRows.Count == 0)
        {
            writer.WriteLine("None.");
        }
        else
        {
            writer.WriteLine("| Scenario | Range | Value | Reason |");
            writer.WriteLine("|---|---|---:|---|");
            foreach (SkippedBenchmarkRow skipped in result.SkippedRows.Take(Math.Max(1, top)))
            {
                writer.WriteLine($"| {skipped.ScenarioName} | {skipped.ValueRangeKind?.ToString() ?? "-"} | {skipped.InputValue?.ToString() ?? "-"} | {skipped.Reason.Replace('|', '/')} |");
            }
        }
        writer.WriteLine();
        writer.WriteLine("## Notes");
        writer.WriteLine("- Raw timings and weighting metadata are reported separately.");
        writer.WriteLine("- Allocation values are quick-mode approximations measured around one round-trip execution.");
    }

    private static void WriteTopResult<T>(
        TextWriter writer,
        string label,
        T? value,
        Func<T, string> formatter)
    {
        writer.WriteLine(value is null ? $"- {label}: n/a" : $"- {label}: {formatter(value)}");
    }
}
