using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public sealed class IdentityPermutation : IPermutation
{
    public PermutationKind Kind => PermutationKind.Identity;

    public ulong Forward(ulong value, CodecParameters parameters) => BitMask.Apply(value, parameters.BitLength);

    public ulong Reverse(ulong value, CodecParameters parameters) => BitMask.Apply(value, parameters.BitLength);
}
