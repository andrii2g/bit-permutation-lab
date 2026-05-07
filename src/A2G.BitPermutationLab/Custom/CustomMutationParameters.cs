using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Custom;

public sealed record CustomMutationParameters(
    string Name,
    CustomMutationPosition Position,
    IReadOnlyDictionary<string, string> Parameters);

public sealed record CustomChunkMutationParameters(
    string Name,
    CustomChunkMutationPosition Position,
    IReadOnlyDictionary<string, string> Parameters);
