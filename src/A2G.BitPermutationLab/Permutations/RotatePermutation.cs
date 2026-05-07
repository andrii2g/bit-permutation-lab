using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class RotatePermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.Rotate;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        int rotateBy = NormalizeRotateBy(parameters);
        return RotateLeftWithinBitLength(value, rotateBy, parameters.BitLength);
    }

    public ulong Reverse(ulong value, CodecParameters parameters)
    {
        int rotateBy = NormalizeRotateBy(parameters);
        return RotateRightWithinBitLength(value, rotateBy, parameters.BitLength);
    }

    public static ulong RotateLeftWithinBitLength(ulong value, int rotateBy, int bitLength)
    {
        ulong masked = BitMask.Apply(value, bitLength);
        if (bitLength <= 0)
        {
            return 0UL;
        }

        int normalized = rotateBy % bitLength;
        if (normalized == 0)
        {
            return masked;
        }

        ulong left = BitMask.Apply(masked << normalized, bitLength);
        ulong right = masked >> (bitLength - normalized);
        return BitMask.Apply(left | right, bitLength);
    }

    public static ulong RotateRightWithinBitLength(ulong value, int rotateBy, int bitLength)
    {
        ulong masked = BitMask.Apply(value, bitLength);
        if (bitLength <= 0)
        {
            return 0UL;
        }

        int normalized = rotateBy % bitLength;
        if (normalized == 0)
        {
            return masked;
        }

        ulong right = masked >> normalized;
        ulong left = BitMask.Apply(masked << (bitLength - normalized), bitLength);
        return BitMask.Apply(left | right, bitLength);
    }

    private static int NormalizeRotateBy(CodecParameters parameters)
    {
        return parameters.Permutation.RotateBy.GetValueOrDefault() % parameters.BitLength;
    }
}
