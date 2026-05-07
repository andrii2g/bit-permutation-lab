using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public sealed record MixerParameters(
    MixerKind Kind,
    SaltDerivationKind MaskDerivation = SaltDerivationKind.SplitMix64,
    ulong? LiteralMask = null,
    ulong? LiteralAddend = null,
    ulong? Multiplier = null);
