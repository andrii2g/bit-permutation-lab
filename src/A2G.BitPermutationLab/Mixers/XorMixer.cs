using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public sealed class XorMixer : IMixer
{
    public MixerKind Kind => MixerKind.Xor;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        ulong mask = ResolveMask(parameters);
        return BitMask.Apply(value ^ mask, parameters.BitLength);
    }

    public ulong Reverse(ulong value, CodecParameters parameters)
    {
        ulong mask = ResolveMask(parameters);
        return BitMask.Apply(value ^ mask, parameters.BitLength);
    }

    private static ulong ResolveMask(CodecParameters parameters)
    {
        return parameters.Mixer.LiteralMask ??
               SaltDerivation.Derive(
                   parameters.SaltSeed,
                   parameters.Mixer.MaskDerivation,
                   SaltDerivation.MixerMaskDomain,
                   parameters.BitLength);
    }
}
