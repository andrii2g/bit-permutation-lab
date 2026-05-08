using System.Globalization;

namespace A2G.BitPermutationLab.Benchmarking;

public static class CsvBenchmarkReportWriter
{
    public static void Write(BenchmarkRunResult result, TextWriter writer)
    {
        writer.WriteLine("ScenarioName,ScenarioId,Profile,Mode,InputType,BinaryKind,BitOrder,ByteOrder,BitLength,ValueRangeKind,ParameterTier,SaltSeed,MinInput,MaxInput,InputValue,MixerKind,MixerParameters,PermutationKind,PermutationParameters,ChunkingKind,ChunkSize,EmitterKind,AlphabetKind,OutputKind,CustomMutationKind,CustomMutationPosition,AlgorithmWeight,ParameterTierWeight,ValueRangeWeight,EmitterWeight,CustomMutationWeight,ExpectedCostFactor,SelectionWeight,OutputLength,EncodeNsPerOp,DecodeNsPerOp,RoundTripNsPerOp,EncodeOpsPerSec,DecodeOpsPerSec,AllocatedBytesPerOp,RoundTripOk,Skipped,SkipReason,Notes");

        foreach (BenchmarkResultRow row in result.Rows)
        {
            writer.WriteLine(string.Join(",",
                Escape(row.ScenarioName),
                Escape(row.ScenarioId),
                Escape(row.Profile),
                Escape(row.Mode),
                Escape(row.NumberKind.ToString()),
                Escape(row.BinaryKind.ToString()),
                Escape(row.BitOrderKind.ToString()),
                Escape(row.ByteOrderKind.ToString()),
                row.BitLength.ToString(CultureInfo.InvariantCulture),
                Escape(row.ValueRangeKind.ToString()),
                Escape(row.ParameterTier.ToString()),
                row.SaltSeed.ToString(CultureInfo.InvariantCulture),
                row.MinInput.ToString(CultureInfo.InvariantCulture),
                row.MaxInput.ToString(CultureInfo.InvariantCulture),
                row.InputValue.ToString(CultureInfo.InvariantCulture),
                Escape(row.MixerKind.ToString()),
                Escape(row.MixerParameters),
                Escape(row.PermutationKind.ToString()),
                Escape(row.PermutationParameters),
                Escape(row.ChunkingKind.ToString()),
                row.ChunkSize.ToString(CultureInfo.InvariantCulture),
                Escape(row.EmitterKind.ToString()),
                Escape(row.AlphabetKind.ToString()),
                Escape(row.OutputKind.ToString()),
                Escape(row.CustomMutationKind),
                Escape(row.CustomMutationPosition),
                row.AlgorithmWeight.ToString("F3", CultureInfo.InvariantCulture),
                row.ParameterTierWeight.ToString("F3", CultureInfo.InvariantCulture),
                row.ValueRangeWeight.ToString("F3", CultureInfo.InvariantCulture),
                row.EmitterWeight.ToString("F3", CultureInfo.InvariantCulture),
                row.CustomMutationWeight.ToString("F3", CultureInfo.InvariantCulture),
                row.ExpectedCostFactor.ToString("F3", CultureInfo.InvariantCulture),
                row.SelectionWeight.ToString("F3", CultureInfo.InvariantCulture),
                row.OutputLength.ToString(CultureInfo.InvariantCulture),
                row.EncodeNanoseconds.ToString("F3", CultureInfo.InvariantCulture),
                row.DecodeNanoseconds.ToString("F3", CultureInfo.InvariantCulture),
                row.RoundTripNanoseconds.ToString("F3", CultureInfo.InvariantCulture),
                row.EncodeOperationsPerSecond.ToString("F3", CultureInfo.InvariantCulture),
                row.DecodeOperationsPerSecond.ToString("F3", CultureInfo.InvariantCulture),
                row.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
                row.RoundTripValid ? "true" : "false",
                "false",
                string.Empty,
                Escape(row.SampleOutput)));
        }

        foreach (SkippedBenchmarkRow skipped in result.SkippedRows)
        {
            string[] columns = new string[44];
            columns[0] = Escape(skipped.ScenarioName);
            columns[1] = Escape(skipped.ScenarioId ?? string.Empty);
            columns[9] = Escape(skipped.ValueRangeKind?.ToString() ?? string.Empty);
            columns[12] = skipped.InputValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            columns[13] = skipped.InputValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            columns[14] = skipped.InputValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            columns[40] = "false";
            columns[41] = "true";
            columns[42] = Escape(skipped.Reason);
            columns[43] = string.Empty;
            writer.WriteLine(string.Join(",", columns));
        }
    }

    private static string Escape(string value)
    {
        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
