using A2G.BitPermutationLab.Benchmarking;

namespace A2G.BitPermutationLab.Tests;

public sealed class BenchmarkReportWriterTests
{
    [Fact]
    public void MarkdownWriter_WritesSummarySections()
    {
        BenchmarkRunResult result = BenchmarkRunner.RunDetailed(BenchmarkExecutionOptions.CreateDefault(BenchmarkProfileKind.Quick, 2));
        StringWriter writer = new();

        MarkdownBenchmarkReportWriter.Write(result, writer, 3);

        string text = writer.ToString();
        Assert.Contains("# Bit Permutation Lab Benchmark Report", text);
        Assert.Contains("## Environment", text);
        Assert.Contains("## Matrix Results", text);
        Assert.Contains("## Skipped Scenarios", text);
    }
}
