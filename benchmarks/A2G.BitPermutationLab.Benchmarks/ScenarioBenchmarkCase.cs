using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Benchmarks;

internal sealed record ScenarioBenchmarkCase(
    string DisplayName,
    ulong InputValue,
    CodecParameters Parameters,
    CodecResult Encoded);
