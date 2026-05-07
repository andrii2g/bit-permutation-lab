using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class BitReversePermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.BitReverse;

    public ulong Forward(ulong value, CodecParameters parameters) =>
        FixedUnsignedBinary.BitReverseWithinBitLength(value, parameters.BitLength);

    public ulong Reverse(ulong value, CodecParameters parameters) =>
        FixedUnsignedBinary.BitReverseWithinBitLength(value, parameters.BitLength);
}
