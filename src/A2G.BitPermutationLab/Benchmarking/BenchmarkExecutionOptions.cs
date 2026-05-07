namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkExecutionOptions(
    string ProfileLabel,
    int Iterations,
    BenchmarkSelectionOptions Selection,
    BenchmarkReportOptions Report)
{
    public static BenchmarkExecutionOptions CreateDefault(BenchmarkProfileKind profileKind, int iterations)
    {
        return new BenchmarkExecutionOptions(
            profileKind.ToString(),
            iterations,
            BenchmarkProfileFactory.CreateSelectionOptions(profileKind),
            BenchmarkReportOptions.Default);
    }
}
