using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public static class MixerFactory
{
    public static IMixer Create(MixerKind kind)
    {
        return kind switch
        {
            MixerKind.None => new NoMixer(),
            MixerKind.Xor => new XorMixer(),
            MixerKind.Add => new AddMixer(),
            MixerKind.Multiply => new MultiplyMixer(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported mixer kind.")
        };
    }
}
