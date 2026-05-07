namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkConsoleFormatter
{
    public static void Write(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        writer.WriteLine("Scenario | Value | Output | Encode ns | Decode ns | Total ns | Valid");
        writer.WriteLine("---|---:|---:|---:|---:|---:|---");

        foreach (BenchmarkResultRow row in rows)
        {
            writer.WriteLine(
                $"{row.ScenarioName} | {row.InputValue} | {row.OutputLength} | {row.EncodeNanoseconds:F1} | {row.DecodeNanoseconds:F1} | {row.RoundTripNanoseconds:F1} | {row.RoundTripValid}");
        }
    }
}
