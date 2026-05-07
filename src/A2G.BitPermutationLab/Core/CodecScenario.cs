namespace A2G.BitPermutationLab.Core;

public sealed record CodecScenario(
    string Name,
    CodecParameters Parameters,
    IReadOnlyList<ulong> Values,
    string? Notes = null);
