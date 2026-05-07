using System.Diagnostics;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkRunner
{
    public static IReadOnlyList<BenchmarkResultRow> Run(BenchmarkProfileKind profileKind, int iterations)
    {
        CodecPipeline pipeline = new();
        List<BenchmarkResultRow> rows = [];

        foreach (BenchmarkScenario scenario in BenchmarkProfileFactory.Create(profileKind))
        {
            foreach (ulong value in scenario.Values)
            {
                CodecResult encoded = pipeline.Encode(value, scenario.Parameters);
                DecodeResult decoded = DecodeWithEncodedResult(pipeline, encoded, scenario.Parameters);

                double encodeNs = Measure(iterations, () => pipeline.Encode(value, scenario.Parameters));
                double decodeNs = Measure(iterations, () => DecodeWithEncodedResult(pipeline, encoded, scenario.Parameters));

                rows.Add(new BenchmarkResultRow(
                    scenario.Name,
                    value,
                    encoded.OutputLength,
                    encodeNs,
                    decodeNs,
                    encodeNs + decodeNs,
                    decoded.Success && decoded.Value == value,
                    RenderSample(encoded)));
            }
        }

        return rows;
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
