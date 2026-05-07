using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public interface IPermutation
{
    PermutationKind Kind { get; }

    ulong Forward(ulong value, CodecParameters parameters);

    ulong Reverse(ulong value, CodecParameters parameters);
}
