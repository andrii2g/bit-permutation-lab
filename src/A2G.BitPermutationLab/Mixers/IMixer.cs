using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public interface IMixer
{
    MixerKind Kind { get; }

    ulong Forward(ulong value, CodecParameters parameters);

    ulong Reverse(ulong value, CodecParameters parameters);
}
