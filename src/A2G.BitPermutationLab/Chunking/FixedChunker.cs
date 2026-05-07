using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Chunking;

public sealed class FixedChunker : IChunker
{
    public ChunkerKind Kind => ChunkerKind.Fixed;

    public int[] Chunk(ulong value, CodecParameters parameters)
    {
        int chunkSize = parameters.Chunking.ChunkSize;
        int bitLength = parameters.BitLength;
        int chunkCount = bitLength / chunkSize;
        int[] chunks = new int[chunkCount];
        ulong masked = BitMask.Apply(value, bitLength);
        ulong chunkMask = BitMask.ForBitLength(chunkSize);

        if (parameters.Chunking.ChunkReadOrder == BitOrderKind.MsbFirst)
        {
            for (int i = 0; i < chunkCount; i++)
            {
                int shift = bitLength - chunkSize * (i + 1);
                chunks[i] = (int)((masked >> shift) & chunkMask);
            }
        }
        else
        {
            for (int i = 0; i < chunkCount; i++)
            {
                int shift = chunkSize * i;
                chunks[i] = (int)((masked >> shift) & chunkMask);
            }
        }

        return chunks;
    }

    public ulong Unchunk(ReadOnlySpan<int> chunks, CodecParameters parameters)
    {
        int chunkSize = parameters.Chunking.ChunkSize;
        int bitLength = parameters.BitLength;
        ulong chunkMask = BitMask.ForBitLength(chunkSize);
        ulong result = 0UL;

        if (parameters.Chunking.ChunkReadOrder == BitOrderKind.MsbFirst)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                int shift = bitLength - chunkSize * (i + 1);
                result |= ((ulong)chunks[i] & chunkMask) << shift;
            }
        }
        else
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                int shift = chunkSize * i;
                result |= ((ulong)chunks[i] & chunkMask) << shift;
            }
        }

        return BitMask.Apply(result, bitLength);
    }
}
