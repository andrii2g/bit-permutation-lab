using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class ByteSwapPermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.ByteSwap;

    public ulong Forward(ulong value, CodecParameters parameters) =>
        FixedUnsignedBinary.ByteSwapWithinBitLength(value, parameters.BitLength);

    public ulong Reverse(ulong value, CodecParameters parameters) =>
        FixedUnsignedBinary.ByteSwapWithinBitLength(value, parameters.BitLength);
}
