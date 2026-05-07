using System.Diagnostics;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Validation;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkRunner
{
    public static IReadOnlyList<BenchmarkResultRow> Run(BenchmarkProfileKind profileKind, int iterations)
    {
        return RunDetailed(BenchmarkExecutionOptions.CreateDefault(profileKind, iterations)).Rows;
    }

    public static IReadOnlyList<BenchmarkResultRow> Run(BenchmarkExecutionOptions options)
    {
        return RunDetailed(options).Rows;
    }

    public static BenchmarkRunResult RunDetailed(BenchmarkExecutionOptions options)
    {
        return RunDetailed(BenchmarkProfileFactory.Create(options.Selection), options);
    }

    public static BenchmarkRunResult RunDetailed(IReadOnlyList<BenchmarkScenario> scenarios, BenchmarkExecutionOptions options)
    {
        CodecPipeline pipeline = new();
        List<BenchmarkResultRow> rows = [];
        List<SkippedBenchmarkRow> skippedRows = [];

        foreach (BenchmarkScenario scenario in scenarios)
        {
            if (options.ValidateScenarios)
            {
                ValidationResult parameterValidation = ParameterValidator.Validate(scenario.Parameters);
                if (!parameterValidation.IsValid)
                {
                    skippedRows.Add(new SkippedBenchmarkRow(
                        scenario.Name,
                        scenario.ScenarioId,
                        scenario.ValueRangeKind,
                        null,
                        string.Join("; ", parameterValidation.Errors.Select(static error => error.Message))));
                    continue;
                }
            }

            foreach (ulong value in scenario.Values)
            {
                if (options.ValidateScenarios)
                {
                    ValidationResult valueValidation = ParameterValidator.ValidateValue(value, scenario.Parameters);
                    if (!valueValidation.IsValid)
                    {
                        skippedRows.Add(new SkippedBenchmarkRow(
                            scenario.Name,
                            scenario.ScenarioId,
                            scenario.ValueRangeKind,
                            value,
                            string.Join("; ", valueValidation.Errors.Select(static error => error.Message))));
                        continue;
                    }
                }

                try
                {
                    CodecResult encoded = pipeline.Encode(value, scenario.Parameters);
                    DecodeResult decoded = DecodeWithEncodedResult(pipeline, encoded, scenario.Parameters);

                    double encodeNs = Measure(options.Iterations, () => pipeline.Encode(value, scenario.Parameters));
                    double decodeNs = Measure(options.Iterations, () => DecodeWithEncodedResult(pipeline, encoded, scenario.Parameters));
                    long allocatedBytes = MeasureAllocatedBytes(() =>
                    {
                        CodecResult allocationEncoded = pipeline.Encode(value, scenario.Parameters);
                        DecodeWithEncodedResult(pipeline, allocationEncoded, scenario.Parameters);
                    });

                    rows.Add(new BenchmarkResultRow(
                        options.ProfileLabel,
                        options.ModeLabel,
                        scenario.ScenarioId,
                        scenario.Name,
                        scenario.ValueRangeKind,
                        scenario.Weights.SelectionWeight,
                        scenario.Weights.ExpectedCostFactor,
                        scenario.Weights.IsRequiredBaseline,
                        scenario.Parameters.BitLength,
                        scenario.Parameters.Mixer.Kind,
                        scenario.Parameters.Permutation.Kind,
                        scenario.Parameters.Chunking.ChunkSize,
                        scenario.Parameters.Emitter.Kind,
                        scenario.Parameters.Emitter.OutputKind,
                        value,
                        encoded.OutputLength,
                        encodeNs,
                        decodeNs,
                        encodeNs + decodeNs,
                        ToOperationsPerSecond(encodeNs),
                        ToOperationsPerSecond(decodeNs),
                        allocatedBytes,
                        decoded.Success && decoded.Value == value,
                        RenderSample(encoded)));
                }
                catch (Exception exception)
                {
                    skippedRows.Add(new SkippedBenchmarkRow(
                        scenario.Name,
                        scenario.ScenarioId,
                        scenario.ValueRangeKind,
                        value,
                        exception.Message));
                }
            }
        }

        return new BenchmarkRunResult(options, rows, skippedRows, DateTimeOffset.UtcNow);
    }

    private static DecodeResult DecodeWithEncodedResult(CodecPipeline pipeline, CodecResult encoded, CodecParameters parameters)
    {
        return encoded.OutputKind == OutputKind.ByteArray
            ? pipeline.Decode(encoded.ByteArrayValue ?? Array.Empty<byte>(), parameters)
            : pipeline.Decode((encoded.StringValue ?? new string(encoded.CharArrayValue ?? Array.Empty<char>())).AsSpan(), parameters);
    }

    private static double Measure(int iterations, Action action)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }

        stopwatch.Stop();
        return stopwatch.ElapsedTicks * (1_000_000_000.0 / Stopwatch.Frequency) / iterations;
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        long after = GC.GetAllocatedBytesForCurrentThread();
        return Math.Max(0L, after - before);
    }

    private static double ToOperationsPerSecond(double nanoseconds)
    {
        return nanoseconds <= 0d ? 0d : 1_000_000_000d / nanoseconds;
    }

    private static string RenderSample(CodecResult encoded)
    {
        return encoded.OutputKind switch
        {
            OutputKind.String => encoded.StringValue ?? string.Empty,
            OutputKind.CharArray => new string(encoded.CharArrayValue ?? Array.Empty<char>()),
            OutputKind.ByteArray => Convert.ToHexString(encoded.ByteArrayValue ?? Array.Empty<byte>()),
            _ => string.Empty
        };
    }
}
