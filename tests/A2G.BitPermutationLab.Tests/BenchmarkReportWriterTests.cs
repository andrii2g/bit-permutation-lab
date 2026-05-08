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

    [Fact]
    public void CsvWriter_EmitsWeightingColumns()
    {
        BenchmarkRunResult result = BenchmarkRunner.RunDetailed(BenchmarkExecutionOptions.CreateDefault(BenchmarkProfileKind.Quick, 2));
        StringWriter writer = new();

        CsvBenchmarkReportWriter.Write(result, writer);

        string text = writer.ToString();
        Assert.Contains("ParameterTier", text);
        Assert.Contains("SaltSeed", text);
        Assert.Contains("AlgorithmWeight", text);
        Assert.Contains("CustomMutationWeight", text);
    }

    [Fact]
    public void CsvWriter_EmitsScenarioRangeBounds()
    {
        BenchmarkRunResult result = BenchmarkRunner.RunDetailed(BenchmarkExecutionOptions.CreateDefault(BenchmarkProfileKind.Quick, 2));
        BenchmarkResultRow firstRow = result.Rows[0];
        StringWriter writer = new();

        CsvBenchmarkReportWriter.Write(result, writer);

        string text = writer.ToString();
        string expectedFragment = $",{firstRow.MinInput},{firstRow.MaxInput},{firstRow.InputValue},";
        Assert.Contains(expectedFragment, text);
    }
}
