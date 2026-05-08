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
            ulong minInput = scenario.Values.Count == 0 ? 0UL : scenario.Values.Min();
            ulong maxInput = scenario.Values.Count == 0 ? 0UL : scenario.Values.Max();

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
                        scenario.Parameters.NumberKind,
                        scenario.Parameters.Binary.Kind,
                        scenario.Parameters.Binary.BitOrder,
                        scenario.Parameters.Binary.ByteOrder,
                        scenario.ValueRangeKind,
                        scenario.ParameterTier,
                        scenario.Parameters.SaltSeed,
                        scenario.Weights.SelectionWeight,
                        scenario.Weights.AlgorithmWeight,
                        scenario.Weights.ParameterTierWeight,
                        scenario.Weights.ValueRangeWeight,
                        scenario.Weights.EmitterWeight,
                        scenario.Weights.CustomMutationWeight,
                        scenario.Weights.ExpectedCostFactor,
                        scenario.Weights.IsRequiredBaseline,
                        scenario.Parameters.BitLength,
                        FormatMixerParameters(scenario.Parameters),
                        scenario.Parameters.Mixer.Kind,
                        FormatPermutationParameters(scenario.Parameters),
                        scenario.Parameters.Permutation.Kind,
                        scenario.Parameters.Chunking.Kind,
                        scenario.Parameters.Chunking.ChunkSize,
                        scenario.Parameters.Emitter.Kind,
                        scenario.Parameters.Emitter.AlphabetKind,
                        scenario.Parameters.Emitter.OutputKind,
                        scenario.Parameters.CustomChunkMutation?.Name ?? scenario.Parameters.CustomMutation?.Name ?? "None",
                        scenario.Parameters.CustomChunkMutation is not null
                            ? scenario.Parameters.CustomChunkMutation.Position.ToString()
                            : scenario.Parameters.CustomMutation?.Position.ToString() ?? "None",
                        minInput,
                        maxInput,
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

    private static string FormatMixerParameters(CodecParameters parameters)
    {
        return parameters.Mixer.Kind switch
        {
            MixerKind.Xor => $"maskDerivation={parameters.Mixer.MaskDerivation}, literalMask={parameters.Mixer.LiteralMask?.ToString() ?? "-"}",
            MixerKind.Add => $"maskDerivation={parameters.Mixer.MaskDerivation}, literalAddend={parameters.Mixer.LiteralAddend?.ToString() ?? "-"}",
            MixerKind.Multiply => $"multiplier={parameters.Mixer.Multiplier?.ToString() ?? "-"}",
            _ => "-"
        };
    }

    private static string FormatPermutationParameters(CodecParameters parameters)
    {
        return parameters.Permutation.Kind switch
        {
            PermutationKind.Rotate => $"rotateBy={parameters.Permutation.RotateBy}",
            PermutationKind.NibbleSwap => $"nibbleSwap={parameters.Permutation.NibbleSwap}",
            PermutationKind.ChunkPermutation => $"groupSize={parameters.Permutation.ChunkPermutationGroupSize}, variant={parameters.Permutation.ChunkPermutationVariant}, rotateBy={parameters.Permutation.ChunkPermutationRotateBy}, order={FormatOrder(parameters.Permutation.ChunkPermutationOrder)}",
            PermutationKind.Feistel => $"rounds={parameters.Permutation.FeistelRounds}, function={parameters.Permutation.FeistelRoundFunction}",
            _ => "-"
        };
    }

    private static string FormatOrder(IReadOnlyList<int>? order)
    {
        return order is null || order.Count == 0 ? "-" : string.Join(",", order);
    }
}
