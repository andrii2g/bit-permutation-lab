using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Mixers;

public sealed class MultiplyMixer : IMixer
{
    public MixerKind Kind => MixerKind.Multiply;

    public ulong Forward(ulong value, CodecParameters parameters)
    {
        ulong multiplier = ResolveMultiplier(parameters);
        return BitMask.Apply(unchecked(value * multiplier), parameters.BitLength);
    }

    public ulong Reverse(ulong value, CodecParameters parameters)
    {
        ulong multiplier = ResolveMultiplier(parameters);
        ulong inverseMultiplier = ComputeInverseMultiplier(multiplier, parameters.BitLength);
        return BitMask.Apply(unchecked(value * inverseMultiplier), parameters.BitLength);
    }

    private static ulong ResolveMultiplier(CodecParameters parameters)
    {
        if (parameters.Mixer.Multiplier is ulong literalMultiplier)
        {
            return literalMultiplier | 1UL;
        }

        ulong derived = SaltDerivation.Derive(
            parameters.SaltSeed,
            parameters.Mixer.MaskDerivation,
            SaltDerivation.MultiplierDomain,
            parameters.BitLength);

        return BitMask.Apply(derived, parameters.BitLength) | 1UL;
    }

    private static ulong ComputeInverseMultiplier(ulong multiplier, int bitLength)
    {
        if (bitLength >= 64)
        {
            return ModularInversePow2(multiplier, 64);
        }

        return BitMask.Apply(ModularInversePow2(multiplier, bitLength), bitLength);
    }

    private static ulong ModularInversePow2(ulong oddValue, int bitLength)
    {
        ulong inverse = oddValue;
        for (int i = 0; i < 6; i++)
        {
            inverse *= unchecked(2UL - (oddValue * inverse));
        }

        return bitLength >= 64 ? inverse : BitMask.Apply(inverse, bitLength);
    }
}
