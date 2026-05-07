using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public sealed class NoMixer : IMixer
{
    public MixerKind Kind => MixerKind.None;

    public ulong Forward(ulong value, CodecParameters parameters) => BitMask.Apply(value, parameters.BitLength);

    public ulong Reverse(ulong value, CodecParameters parameters) => BitMask.Apply(value, parameters.BitLength);
}
