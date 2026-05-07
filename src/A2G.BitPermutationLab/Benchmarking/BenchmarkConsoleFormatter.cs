namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkConsoleFormatter
{
    public static void Write(IReadOnlyList<BenchmarkResultRow> rows, TextWriter writer)
    {
        writer.WriteLine("Scenario | Range | Weight | Baseline | Value | Output | Encode ns | Decode ns | Total ns | Valid");
        writer.WriteLine("---|---|---:|---|---:|---:|---:|---:|---:|---");

        foreach (BenchmarkResultRow row in rows)
        {
            writer.WriteLine(
                $"{row.ScenarioName} | {row.ValueRangeKind} | {row.SelectionWeight:F3} | {row.IsRequiredBaseline} | {row.InputValue} | {row.OutputLength} | {row.EncodeNanoseconds:F1} | {row.DecodeNanoseconds:F1} | {row.RoundTripNanoseconds:F1} | {row.RoundTripValid}");
        }
    }
}
