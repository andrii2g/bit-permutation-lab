using A2G.BitPermutationLab.Benchmarking;
using A2G.BitPermutationLab.Core;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace A2G.BitPermutationLab.Benchmarks;

internal static class BenchmarkDotNetSummaryConverter
{
    public static BenchmarkRunResult Convert(
        Summary summary,
        BenchmarkExecutionOptions options)
    {
        Dictionary<string, ScenarioMeasurement> measurements = new(StringComparer.Ordinal);

        foreach (BenchmarkReport report in summary.Reports)
        {
            if (!TryGetScenarioCase(report.BenchmarkCase, out ScenarioBenchmarkCase? scenarioCase))
            {
                continue;
            }

            ScenarioBenchmarkCase typedCase = scenarioCase!;

            if (!measurements.TryGetValue(typedCase.DisplayName, out ScenarioMeasurement? measurement))
            {
                measurement = new ScenarioMeasurement(typedCase);
                measurements[typedCase.DisplayName] = measurement;
            }

            string methodName = report.BenchmarkCase.Descriptor.WorkloadMethod?.Name ?? string.Empty;
            double meanNanoseconds = report.ResultStatistics?.Mean ?? 0d;
            long allocatedBytes = report.GcStats.GetTotalAllocatedBytes(true) ?? 0L;

            switch (methodName)
            {
                case nameof(ScenarioBenchmarks.Encode):
                    measurement.EncodeNanoseconds = meanNanoseconds;
                    measurement.EncodeOperationsPerSecond = ToOperationsPerSecond(meanNanoseconds);
                    measurement.AllocatedBytes = allocatedBytes;
                    break;
                case nameof(ScenarioBenchmarks.Decode):
                    measurement.DecodeNanoseconds = meanNanoseconds;
                    measurement.DecodeOperationsPerSecond = ToOperationsPerSecond(meanNanoseconds);
                    measurement.AllocatedBytes = Math.Max(
                        measurement.AllocatedBytes,
                        allocatedBytes);
                    break;
            }
        }

        List<BenchmarkResultRow> rows = [.. measurements.Values
            .Select(measurement => measurement.ToRow(options))
            .OrderBy(static row => row.ScenarioId, StringComparer.Ordinal)
            .ThenBy(static row => row.InputValue)];

        return new BenchmarkRunResult(options, rows, [], DateTimeOffset.UtcNow);
    }

    private static bool TryGetScenarioCase(BenchmarkCase benchmarkCase, out ScenarioBenchmarkCase? scenarioCase)
    {
        foreach (var parameter in benchmarkCase.Parameters.Items)
        {
            if (parameter.Value is ScenarioBenchmarkCase typed)
            {
                scenarioCase = typed;
                return true;
            }
        }

        scenarioCase = null;
        return false;
    }

    private static double ToOperationsPerSecond(double nanoseconds)
    {
        return nanoseconds <= 0d ? 0d : 1_000_000_000d / nanoseconds;
    }

    private sealed class ScenarioMeasurement
    {
        public ScenarioMeasurement(ScenarioBenchmarkCase scenarioCase)
        {
            ScenarioCase = scenarioCase;
        }

        public ScenarioBenchmarkCase ScenarioCase { get; }

        public double EncodeNanoseconds { get; set; }

        public double DecodeNanoseconds { get; set; }

        public double EncodeOperationsPerSecond { get; set; }

        public double DecodeOperationsPerSecond { get; set; }

        public long AllocatedBytes { get; set; }

        public BenchmarkResultRow ToRow(BenchmarkExecutionOptions options)
        {
            return new BenchmarkResultRow(
                options.ProfileLabel,
                options.ModeLabel,
                ScenarioCase.ScenarioId,
                ScenarioCase.ScenarioName,
                ScenarioCase.Parameters.NumberKind,
                ScenarioCase.Parameters.Binary.Kind,
                ScenarioCase.Parameters.Binary.BitOrder,
                ScenarioCase.Parameters.Binary.ByteOrder,
                ScenarioCase.ValueRangeKind,
                ParameterTierKind.Explicit,
                ScenarioCase.Parameters.SaltSeed,
                ScenarioCase.SelectionWeight,
                ScenarioCase.SelectionWeight,
                1.00,
                ScenarioCase.ValueRangeKind switch
                {
                    ValueRangeKind.Tiny => 0.50,
                    _ => 1.00
                },
                1.00,
                ScenarioCase.Parameters.CustomMutation is null && ScenarioCase.Parameters.CustomChunkMutation is null ? 1.00 : 1.20,
                ScenarioCase.ExpectedCostFactor,
                ScenarioCase.IsRequiredBaseline,
                ScenarioCase.Parameters.BitLength,
                FormatMixerParameters(ScenarioCase.Parameters),
                ScenarioCase.Parameters.Mixer.Kind,
                FormatPermutationParameters(ScenarioCase.Parameters),
                ScenarioCase.Parameters.Permutation.Kind,
                ScenarioCase.Parameters.Chunking.Kind,
                ScenarioCase.Parameters.Chunking.ChunkSize,
                ScenarioCase.Parameters.Emitter.Kind,
                ScenarioCase.Parameters.Emitter.AlphabetKind,
                ScenarioCase.Parameters.Emitter.OutputKind,
                ScenarioCase.Parameters.CustomChunkMutation?.Name ?? ScenarioCase.Parameters.CustomMutation?.Name ?? "None",
                ScenarioCase.Parameters.CustomChunkMutation is not null
                    ? ScenarioCase.Parameters.CustomChunkMutation.Position.ToString()
                    : ScenarioCase.Parameters.CustomMutation?.Position.ToString() ?? "None",
                ScenarioCase.InputValue,
                ScenarioCase.Encoded.OutputLength,
                EncodeNanoseconds,
                DecodeNanoseconds,
                EncodeNanoseconds + DecodeNanoseconds,
                EncodeOperationsPerSecond,
                DecodeOperationsPerSecond,
                AllocatedBytes,
                true,
                RenderSample(ScenarioCase.Encoded));
        }

        private static string RenderSample(CodecResult encoded)
        {
            return encoded.OutputKind switch
            {
                OutputKind.String => encoded.StringValue ?? string.Empty,
                OutputKind.CharArray => new string(encoded.CharArrayValue ?? Array.Empty<char>()),
                OutputKind.ByteArray => System.Convert.ToHexString(encoded.ByteArrayValue ?? Array.Empty<byte>()),
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
}
