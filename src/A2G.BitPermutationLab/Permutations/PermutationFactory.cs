using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Permutations;

public static class PermutationFactory
{
    public static IPermutation Create(PermutationKind kind)
    {
        return kind switch
        {
            PermutationKind.Identity => new IdentityPermutation(),
            PermutationKind.Not => new NotPermutation(),
            PermutationKind.Rotate => new RotatePermutation(),
            PermutationKind.ByteSwap => new ByteSwapPermutation(),
            PermutationKind.BitReverse => new BitReversePermutation(),
            PermutationKind.NibbleSwap => new NibbleSwapPermutation(),
            PermutationKind.ChunkPermutation => new ChunkPermutation(),
            PermutationKind.Feistel => new FeistelPermutation(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported permutation kind in this iteration.")
        };
    }
}
