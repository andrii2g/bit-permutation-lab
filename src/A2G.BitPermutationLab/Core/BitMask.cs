namespace A2G.BitPermutationLab.Core;

public static class BitMask
{
    public static ulong ForBitLength(int bitLength)
    {
        return bitLength switch
        {
            <= 0 => 0UL,
            >= 64 => ulong.MaxValue,
            _ => (1UL << bitLength) - 1UL
        };
    }

    public static ulong Apply(ulong value, int bitLength) => value & ForBitLength(bitLength);
}
