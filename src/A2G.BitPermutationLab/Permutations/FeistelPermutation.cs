using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class FeistelPermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.Feistel;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        int bitLength = parameters.BitLength;
        int halfBits = bitLength / 2;
        ulong halfMask = BitMask.ForBitLength(halfBits);
        int rounds = parameters.Permutation.FeistelRounds.GetValueOrDefault();
        ulong[] roundKeys = CreateRoundKeys(parameters.SaltSeed, bitLength, rounds, halfMask);

        ulong masked = BitMask.Apply(value, bitLength);
        ulong left = (masked >> halfBits) & halfMask;
        ulong right = masked & halfMask;

        for (int roundIndex = 0; roundIndex < rounds; roundIndex++)
        {
            ulong f = RoundFunction(right, roundKeys[roundIndex], roundIndex, halfBits, halfMask, parameters);
            ulong nextLeft = right;
            ulong nextRight = (left ^ f) & halfMask;
            left = nextLeft;
            right = nextRight;
        }

        return BitMask.Apply((left << halfBits) | right, bitLength);
    }

    public ulong Reverse(ulong value, CodecParameters parameters)
    {
        int bitLength = parameters.BitLength;
        int halfBits = bitLength / 2;
        ulong halfMask = BitMask.ForBitLength(halfBits);
        int rounds = parameters.Permutation.FeistelRounds.GetValueOrDefault();
        ulong[] roundKeys = CreateRoundKeys(parameters.SaltSeed, bitLength, rounds, halfMask);

        ulong masked = BitMask.Apply(value, bitLength);
        ulong left = (masked >> halfBits) & halfMask;
        ulong right = masked & halfMask;

        for (int roundIndex = rounds - 1; roundIndex >= 0; roundIndex--)
        {
            ulong previousRight = left;
            ulong f = RoundFunction(previousRight, roundKeys[roundIndex], roundIndex, halfBits, halfMask, parameters);
            ulong previousLeft = (right ^ f) & halfMask;
            left = previousLeft;
            right = previousRight;
        }

        return BitMask.Apply((left << halfBits) | right, bitLength);
    }

    private static ulong[] CreateRoundKeys(ulong saltSeed, int bitLength, int rounds, ulong halfMask)
    {
        ulong state = saltSeed + SaltDerivation.FeistelDomain + (ulong)bitLength + (ulong)rounds;
        ulong[] roundKeys = new ulong[rounds];

        for (int i = 0; i < rounds; i++)
        {
            roundKeys[i] = SaltDerivation.SplitMix64Next(ref state) & halfMask;
        }

        return roundKeys;
    }

    private static ulong RoundFunction(
        ulong right,
        ulong key,
        int roundIndex,
        int halfBits,
        ulong halfMask,
        CodecParameters parameters)
    {
        return parameters.Permutation.FeistelRoundFunction switch
        {
            FeistelRoundFunctionKind.XorShiftAdd => XorShiftAdd(right, key, roundIndex, halfBits, halfMask),
            FeistelRoundFunctionKind.MultiplyXor => MultiplyXor(right, key, roundIndex, halfBits, halfMask),
            _ => throw new InvalidOperationException("Feistel permutation requires a round function.")
        };
    }

    private static ulong XorShiftAdd(ulong right, ulong key, int roundIndex, int halfBits, ulong halfMask)
    {
        ulong y = (right ^ key) & halfMask;
        int shift = Math.Min(13, halfBits - 1);
        y ^= y >> shift;
        y = (y + RotatePermutation.RotateLeftWithinBitLength(key, roundIndex + 1, halfBits)) & halfMask;
        return y;
    }

    private static ulong MultiplyXor(ulong right, ulong key, int roundIndex, int halfBits, ulong halfMask)
    {
        ulong odd = (key | 1UL) & halfMask;
        if (odd == 0UL)
        {
            odd = 1UL;
        }

        ulong y = (right * odd) & halfMask;
        y ^= RotatePermutation.RotateLeftWithinBitLength(key, roundIndex + 3, halfBits);
        return y & halfMask;
    }
}
