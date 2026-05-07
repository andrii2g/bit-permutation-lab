using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Chunking;

public sealed record ChunkingParameters(
    ChunkerKind Kind,
    int ChunkSize,
    BitOrderKind ChunkReadOrder = BitOrderKind.MsbFirst);
