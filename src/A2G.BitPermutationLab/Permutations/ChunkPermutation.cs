using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class ChunkPermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.ChunkPermutation;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        int[] order = CreateOrder(parameters);
        return ApplyOrder(value, parameters.BitLength, parameters.Permutation.ChunkPermutationGroupSize!.Value, order);
    }

    public ulong Reverse(ulong value, CodecParameters parameters)
    {
        int[] order = CreateOrder(parameters);
        int[] inverseOrder = CreateInverseOrder(order);
        return ApplyOrder(value, parameters.BitLength, parameters.Permutation.ChunkPermutationGroupSize!.Value, inverseOrder);
    }

    private static ulong ApplyOrder(ulong value, int bitLength, int groupSize, IReadOnlyList<int> order)
    {
        ulong masked = BitMask.Apply(value, bitLength);
        int groupCount = bitLength / groupSize;
        ulong[] groups = new ulong[groupCount];
        ulong result = 0UL;
        ulong groupMask = groupSize >= 64 ? ulong.MaxValue : (1UL << groupSize) - 1UL;

        for (int i = 0; i < groupCount; i++)
        {
            int shift = bitLength - groupSize * (i + 1);
            groups[i] = (masked >> shift) & groupMask;
        }

        for (int i = 0; i < groupCount; i++)
        {
            int shift = bitLength - groupSize * (i + 1);
            result |= groups[order[i]] << shift;
        }

        return BitMask.Apply(result, bitLength);
    }

    private static int[] CreateOrder(CodecParameters parameters)
    {
        int bitLength = parameters.BitLength;
        int groupSize = parameters.Permutation.ChunkPermutationGroupSize!.Value;
        int groupCount = bitLength / groupSize;

        return parameters.Permutation.ChunkPermutationVariant switch
        {
            ChunkPermutationVariant.ExplicitOrder => parameters.Permutation.ChunkPermutationOrder?.ToArray()
                ?? throw new InvalidOperationException("Explicit chunk permutation requires an order."),
            ChunkPermutationVariant.ReverseGroups => Enumerable.Range(0, groupCount).Reverse().ToArray(),
            ChunkPermutationVariant.RotateGroupsLeft => CreateRotateLeftOrder(groupCount, parameters.Permutation.ChunkPermutationRotateBy.GetValueOrDefault()),
            ChunkPermutationVariant.RotateGroupsRight => CreateRotateRightOrder(groupCount, parameters.Permutation.ChunkPermutationRotateBy.GetValueOrDefault()),
            ChunkPermutationVariant.SwapAdjacentGroups => CreateSwapAdjacentOrder(groupCount),
            ChunkPermutationVariant.SaltShuffle => CreateSaltShuffleOrder(parameters.SaltSeed, bitLength, groupSize, groupCount),
            _ => throw new InvalidOperationException("Chunk permutation requires a supported variant.")
        };
    }

    private static int[] CreateInverseOrder(IReadOnlyList<int> order)
    {
        int[] inverse = new int[order.Count];
        for (int i = 0; i < order.Count; i++)
        {
            inverse[order[i]] = i;
        }

        return inverse;
    }

    private static int[] CreateRotateLeftOrder(int groupCount, int rotateBy)
    {
        int normalized = rotateBy % groupCount;
        int[] order = new int[groupCount];
        for (int i = 0; i < groupCount; i++)
        {
            order[i] = (i + normalized) % groupCount;
        }

        return order;
    }

    private static int[] CreateRotateRightOrder(int groupCount, int rotateBy)
    {
        int normalized = rotateBy % groupCount;
        int[] order = new int[groupCount];
        for (int i = 0; i < groupCount; i++)
        {
            order[i] = (i - normalized + groupCount) % groupCount;
        }

        return order;
    }

    private static int[] CreateSwapAdjacentOrder(int groupCount)
    {
        int[] order = new int[groupCount];
        for (int i = 0; i < groupCount; i += 2)
        {
            order[i] = i + 1;
            order[i + 1] = i;
        }

        return order;
    }

    private static int[] CreateSaltShuffleOrder(ulong saltSeed, int bitLength, int groupSize, int groupCount)
    {
        ulong state = saltSeed + SaltDerivation.ChunkShuffleDomain + (ulong)bitLength + (ulong)groupSize;
        int[] order = Enumerable.Range(0, groupCount).ToArray();

        for (int i = groupCount - 1; i >= 1; i--)
        {
            ulong random = SaltDerivation.SplitMix64Next(ref state);
            int j = (int)(random % (ulong)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }

        return order;
    }
}
