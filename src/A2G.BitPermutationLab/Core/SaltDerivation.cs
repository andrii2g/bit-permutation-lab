namespace A2G.BitPermutationLab.Core;

public static class SaltDerivation
{
    public const ulong MixerMaskDomain = 0x4D495845525F4D31UL;
    public const ulong AddendDomain = 0x4144445F4D315FUL;
    public const ulong MultiplierDomain = 0x4D554C5F4D315FUL;
    public const ulong FeistelDomain = 0x4645495354454CUL;
    public const ulong ChunkShuffleDomain = 0x4348554E4B5F53UL;

    public static ulong SplitMix64Next(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public static ulong Derive(ulong saltSeed, SaltDerivationKind kind, ulong domainConstant, int bitLength)
    {
        ulong value = kind switch
        {
            SaltDerivationKind.None => 0UL,
            SaltDerivationKind.UseSaltSeedDirectly => saltSeed,
            SaltDerivationKind.SplitMix64 => DeriveSplitMix64(saltSeed, domainConstant),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported salt derivation kind.")
        };

        return BitMask.Apply(value, bitLength);
    }

    public static ulong DeriveSaltSeedFromText(string saltText)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offsetBasis;
        foreach (byte b in System.Text.Encoding.UTF8.GetBytes(saltText))
        {
            hash ^= b;
            hash = unchecked(hash * prime);
        }

        return hash;
    }

    private static ulong DeriveSplitMix64(ulong saltSeed, ulong domainConstant)
    {
        ulong state = saltSeed + domainConstant;
        return SplitMix64Next(ref state);
    }
}
