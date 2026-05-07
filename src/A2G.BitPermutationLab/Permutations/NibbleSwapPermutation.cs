using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class NibbleSwapPermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.NibbleSwap;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        return parameters.Permutation.NibbleSwap switch
        {
            NibbleSwapKind.ReverseNibbles => ReverseNibbles(value, parameters.BitLength),
            NibbleSwapKind.SwapAdjacentNibbles => SwapAdjacentNibbles(value, parameters.BitLength),
            _ => throw new InvalidOperationException("Nibble swap permutation requires a nibble swap mode.")
        };
    }

    public ulong Reverse(ulong value, CodecParameters parameters) => Forward(value, parameters);

    private static ulong ReverseNibbles(ulong value, int bitLength)
    {
        ulong masked = BitMask.Apply(value, bitLength);
        int nibbleCount = bitLength / 4;
        ulong result = 0UL;

        for (int i = 0; i < nibbleCount; i++)
        {
            int sourceShift = i * 4;
            ulong nibble = (masked >> sourceShift) & 0xFUL;
            int targetShift = (nibbleCount - 1 - i) * 4;
            result |= nibble << targetShift;
        }

        return BitMask.Apply(result, bitLength);
    }

    private static ulong SwapAdjacentNibbles(ulong value, int bitLength)
    {
        ulong masked = BitMask.Apply(value, bitLength);
        int nibbleCount = bitLength / 4;
        ulong result = 0UL;

        for (int pair = 0; pair < nibbleCount; pair += 2)
        {
            int firstShift = pair * 4;
            int secondShift = (pair + 1) * 4;
            ulong firstNibble = (masked >> firstShift) & 0xFUL;
            ulong secondNibble = (masked >> secondShift) & 0xFUL;

            result |= firstNibble << secondShift;
            result |= secondNibble << firstShift;
        }

        return BitMask.Apply(result, bitLength);
    }
}
