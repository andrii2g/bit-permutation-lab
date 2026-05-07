using A2G.BitPermutationLab.Benchmarking;
using A2G.BitPermutationLab.Core;
using BenchmarkDotNet.Attributes;

namespace A2G.BitPermutationLab.Benchmarks;

[MemoryDiagnoser]
public sealed class ScenarioBenchmarks
{
    private readonly CodecPipeline _pipeline = new();

    [ParamsSource(nameof(Cases))]
    public ScenarioBenchmarkCase BenchmarkCase { get; set; } = null!;

    public IEnumerable<ScenarioBenchmarkCase> Cases { get; set; } = Array.Empty<ScenarioBenchmarkCase>();

    [Benchmark]
    public CodecResult Encode()
    {
        return _pipeline.Encode(BenchmarkCase.InputValue, BenchmarkCase.Parameters);
    }

    [Benchmark]
    public DecodeResult Decode()
    {
        CodecResult encoded = BenchmarkCase.Encoded;
        return encoded.OutputKind == OutputKind.ByteArray
            ? _pipeline.Decode(encoded.ByteArrayValue ?? Array.Empty<byte>(), BenchmarkCase.Parameters)
            : _pipeline.Decode((encoded.StringValue ?? new string(encoded.CharArrayValue ?? Array.Empty<char>())).AsSpan(), BenchmarkCase.Parameters);
    }
}
