using System.Globalization;

namespace A2G.BitPermutationLab.Benchmarking;

public static class CsvBenchmarkReportWriter
{
    public static void Write(BenchmarkRunResult result, TextWriter writer)
    {
        writer.WriteLine("ScenarioName,ScenarioId,Profile,Mode,InputRange,InputValue,SaltSeed,BitLength,MixLogic,PermutationLogic,ChunkSize,Emitter,OutputKind,OutputLength,EncodeNs,DecodeNs,RoundTripNs,OpsPerSecond,AllocatedBytes,RoundTripValid,Skipped,SkipReason,Notes");

        foreach (BenchmarkResultRow row in result.Rows)
        {
            writer.WriteLine(string.Join(",",
                Escape(row.ScenarioName),
                Escape(row.ScenarioId),
                Escape(row.Profile),
                Escape(row.Mode),
                Escape(row.ValueRangeKind.ToString()),
                row.InputValue.ToString(CultureInfo.InvariantCulture),
                string.Empty,
                row.BitLength.ToString(CultureInfo.InvariantCulture),
                Escape(row.MixerKind.ToString()),
                Escape(row.PermutationKind.ToString()),
                row.ChunkSize.ToString(CultureInfo.InvariantCulture),
                Escape(row.EmitterKind.ToString()),
                Escape(row.OutputKind.ToString()),
                row.OutputLength.ToString(CultureInfo.InvariantCulture),
                row.EncodeNanoseconds.ToString("F3", CultureInfo.InvariantCulture),
                row.DecodeNanoseconds.ToString("F3", CultureInfo.InvariantCulture),
                row.RoundTripNanoseconds.ToString("F3", CultureInfo.InvariantCulture),
                row.EncodeOperationsPerSecond.ToString("F3", CultureInfo.InvariantCulture),
                row.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                row.RoundTripValid ? "true" : "false",
                "false",
                string.Empty,
                Escape(row.SampleOutput)));
        }

        foreach (SkippedBenchmarkRow skipped in result.SkippedRows)
        {
            writer.WriteLine(string.Join(",",
                Escape(skipped.ScenarioName),
                Escape(skipped.ScenarioId ?? string.Empty),
                string.Empty,
                string.Empty,
                Escape(skipped.ValueRangeKind?.ToString() ?? string.Empty),
                skipped.InputValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "false",
                "true",
                Escape(skipped.Reason),
                string.Empty));
        }
    }

    private static string Escape(string value)
    {
        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
