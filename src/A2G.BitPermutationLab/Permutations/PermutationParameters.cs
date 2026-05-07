using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed record PermutationParameters(
    PermutationKind Kind,
    int? RotateBy = null,
    NibbleSwapKind? NibbleSwap = null,
    int? ChunkPermutationGroupSize = null,
    ChunkPermutationVariant? ChunkPermutationVariant = null,
    IReadOnlyList<int>? ChunkPermutationOrder = null,
    int? ChunkPermutationRotateBy = null,
    int? FeistelRounds = null,
    FeistelRoundFunctionKind? FeistelRoundFunction = null);
