using System.Text;
using A2G.BitPermutationLab.Cli;

namespace A2G.BitPermutationLab.Tests;

public sealed class SimplifiedCliTests
{
    [Fact]
    public void Encode_SimpleScenario_WritesEncodedValue()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "encode",
            "--value", "12345",
            "--number-kind", "uint32",
            "--bits", "32",
            "--salt", "42",
            "--mix", "xor",
            "--permute", "rotate",
            "--chunk-size", "4",
            "--emitter", "hex16",
            "--alphabet", "hex16",
            "--output-kind", "string"
        ], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Encoded:", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void Decode_SimpleScenario_RoundTripsKnownValue()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "decode",
            "--value", "7B0E8450",
            "--number-kind", "uint32",
            "--bits", "32",
            "--salt", "42",
            "--mix", "xor",
            "--permute", "rotate",
            "--chunk-size", "4",
            "--emitter", "hex16",
            "--alphabet", "hex16",
            "--output-kind", "string"
        ], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Decoded: 12345", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void Rejects_AdvancedRotateByFlag()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "encode",
            "--value", "12345",
            "--number-kind", "uint32",
            "--bits", "32",
            "--salt", "42",
            "--mix", "xor",
            "--permute", "rotate",
            "--rotate-by", "7",
            "--chunk-size", "4",
            "--emitter", "hex16"
        ], stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("intentionally not exposed", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_FeistelScenario_OnSimplifiedCli()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "encode",
            "--value", "12345",
            "--number-kind", "uint32",
            "--bits", "32",
            "--salt", "42",
            "--mix", "xor",
            "--permute", "feistel",
            "--chunk-size", "4",
            "--emitter", "hex16"
        ], stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("does not expose chunk permutation or Feistel", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ByteArrayScenario_EncodesAndDecodes()
    {
        var encodeStdout = new StringWriter(new StringBuilder());
        var encodeStderr = new StringWriter(new StringBuilder());

        int encodeExitCode = CliApplication.Run(
        [
            "encode",
            "--value", "12345",
            "--number-kind", "uint64",
            "--bits", "64",
            "--salt", "0",
            "--mix", "none",
            "--permute", "identity",
            "--chunk-size", "8",
            "--emitter", "bytes",
            "--alphabet", "none",
            "--output-kind", "byte-array",
            "--byte-array-format", "hex"
        ], encodeStdout, encodeStderr);

        Assert.Equal(0, encodeExitCode);
        string encoded = encodeStdout.ToString().Trim();
        Assert.StartsWith("EncodedBytesHex: ", encoded, StringComparison.Ordinal);

        string payload = encoded["EncodedBytesHex: ".Length..];
        var decodeStdout = new StringWriter(new StringBuilder());
        var decodeStderr = new StringWriter(new StringBuilder());

        int decodeExitCode = CliApplication.Run(
        [
            "decode",
            "--value", payload,
            "--number-kind", "uint64",
            "--bits", "64",
            "--salt", "0",
            "--mix", "none",
            "--permute", "identity",
            "--chunk-size", "8",
            "--emitter", "bytes",
            "--alphabet", "none",
            "--output-kind", "byte-array",
            "--byte-array-format", "hex"
        ], decodeStdout, decodeStderr);

        Assert.Equal(0, decodeExitCode);
        Assert.Contains("Decoded: 12345", decodeStdout.ToString());
    }

    [Fact]
    public void List_WritesSupportedValues()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(["list"], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Available scenario components", stdout.ToString());
        Assert.Contains("Benchmark profiles", stdout.ToString());
        Assert.Contains("Registered custom mutations", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void Benchmark_QuickProfile_WritesRows()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "benchmark",
            "--profile", "quick",
            "--iterations", "5"
        ], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Profile: Quick", stdout.ToString());
        Assert.Contains("Raw Performance Matrix", stdout.ToString());
        Assert.Contains("Weighting Metadata", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void Benchmark_CanWriteWeightedOnlyReport()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "benchmark",
            "--profile", "quick",
            "--iterations", "5",
            "--report-weighted", "true",
            "--report-unweighted", "false"
        ], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("Weighting Metadata", stdout.ToString());
        Assert.DoesNotContain("Raw Performance Matrix", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void Benchmark_AcceptsWeightingOptions()
    {
        var stdout = new StringWriter(new StringBuilder());
        var stderr = new StringWriter(new StringBuilder());

        int exitCode = CliApplication.Run(
        [
            "benchmark",
            "--profile", "default",
            "--weighting-profile", "exploratory",
            "--scenario-budget", "4",
            "--sampling-seed", "77",
            "--include-required-baselines", "false",
            "--iterations", "5"
        ], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Contains("WeightingProfile: Exploratory", stdout.ToString());
        Assert.Contains("ScenarioBudget: 4", stdout.ToString());
        Assert.Contains("SamplingSeed: 77", stdout.ToString());
        Assert.Contains("IncludeRequiredBaselines: False", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void Benchmark_Config_WritesMarkdownAndCsvReports()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "bit-permutation-lab-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string configPath = Path.Combine(tempDirectory, "benchmark.json");
        string markdownPath = Path.Combine(tempDirectory, "report.md");
        string csvPath = Path.Combine(tempDirectory, "report.csv");

        try
        {
            File.WriteAllText(configPath,
                """
                {
                  "scenarios": [
                    {
                      "name": "xor-rotate-hex32",
                      "values": [1, 2, 1000],
                      "parameters": {
                        "name": "xor-rotate-hex32",
                        "numberKind": "UInt32",
                        "bitLength": 32,
                        "saltSeed": 42,
                        "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
                        "mixer": { "kind": "Xor", "maskDerivation": "SplitMix64" },
                        "permutation": { "kind": "Rotate", "rotateBy": 11 },
                        "chunking": { "kind": "Fixed", "chunkSize": 4, "chunkReadOrder": "MsbFirst" },
                        "emitter": { "kind": "Hex16", "alphabetKind": "Hex16", "outputKind": "String", "byteArrayTextFormat": "Hex" }
                      }
                    }
                  ],
                  "benchmark": {
                    "mode": "Quick",
                    "iterations": 5,
                    "validate": true
                  }
                }
                """);

            var stdout = new StringWriter(new StringBuilder());
            var stderr = new StringWriter(new StringBuilder());

            int exitCode = CliApplication.Run(
            [
                "benchmark",
                "--config", configPath,
                "--output", markdownPath,
                "--csv", csvPath
            ], stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(markdownPath));
            Assert.True(File.Exists(csvPath));
            Assert.Contains("MarkdownReport:", stdout.ToString());
            Assert.Contains("CsvReport:", stdout.ToString());
            Assert.Equal(string.Empty, stderr.ToString());
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}
