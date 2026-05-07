using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Chunking;

public interface IChunker
{
    ChunkerKind Kind { get; }

    int[] Chunk(ulong value, CodecParameters parameters);

    ulong Unchunk(ReadOnlySpan<int> chunks, CodecParameters parameters);
}
