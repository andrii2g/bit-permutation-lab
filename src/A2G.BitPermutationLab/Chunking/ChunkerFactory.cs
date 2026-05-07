using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Chunking;

public static class ChunkerFactory
{
    public static IChunker Create(ChunkerKind kind)
    {
        return kind switch
        {
            ChunkerKind.Fixed => new FixedChunker(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported chunker kind.")
        };
    }
}
