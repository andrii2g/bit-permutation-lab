namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkReportOptions(
    bool IncludeWeightedReport,
    bool IncludeUnweightedReport)
{
    public static BenchmarkReportOptions Default { get; } = new(true, true);
}
