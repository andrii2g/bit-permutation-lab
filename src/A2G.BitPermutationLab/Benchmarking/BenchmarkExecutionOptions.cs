namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkExecutionOptions(
    string ProfileLabel,
    string ModeLabel,
    int Iterations,
    BenchmarkSelectionOptions Selection,
    BenchmarkReportOptions Report,
    bool ValidateScenarios = true)
{
    public static BenchmarkExecutionOptions CreateDefault(BenchmarkProfileKind profileKind, int iterations)
    {
        return new BenchmarkExecutionOptions(
            profileKind.ToString(),
            "Quick",
            iterations,
            BenchmarkProfileFactory.CreateSelectionOptions(profileKind),
            BenchmarkReportOptions.Default,
            true);
    }
}
