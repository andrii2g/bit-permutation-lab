using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public sealed class AddMixer : IMixer
{
    public MixerKind Kind => MixerKind.Add;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        ulong addend = ResolveAddend(parameters);
        return BitMask.Apply(unchecked(value + addend), parameters.BitLength);
    }

    public ulong Reverse(ulong value, CodecParameters parameters)
    {
        ulong addend = ResolveAddend(parameters);
        return BitMask.Apply(unchecked(value - addend), parameters.BitLength);
    }

    private static ulong ResolveAddend(CodecParameters parameters)
    {
        return parameters.Mixer.LiteralAddend ??
               SaltDerivation.Derive(
                   parameters.SaltSeed,
                   parameters.Mixer.MaskDerivation,
                   SaltDerivation.AddendDomain,
                   parameters.BitLength);
    }
}
