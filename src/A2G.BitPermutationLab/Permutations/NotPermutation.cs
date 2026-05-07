using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class NotPermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.Not;

    public ulong Forward(ulong value, CodecParameters parameters) =>
        BitMask.Apply(~value, parameters.BitLength);

    public ulong Reverse(ulong value, CodecParameters parameters) =>
        BitMask.Apply(~value, parameters.BitLength);
}
