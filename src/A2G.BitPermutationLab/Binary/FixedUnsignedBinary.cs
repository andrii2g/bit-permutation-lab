using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Binary;

public static class FixedUnsignedBinary
{
    public static ulong ToBinary(ulong value, CodecParameters parameters)
    {
        ulong raw = BitMask.Apply(value, parameters.BitLength);

        if (parameters.Binary.ByteOrder == ByteOrderKind.LittleEndian)
        {
            raw = ByteSwapWithinBitLength(raw, parameters.BitLength);
        }

        if (parameters.Binary.BitOrder == BitOrderKind.LsbFirst)
        {
            raw = BitReverseWithinBitLength(raw, parameters.BitLength);
        }

        return BitMask.Apply(raw, parameters.BitLength);
    }

    public static ulong ToInteger(ulong value, CodecParameters parameters)
    {
        ulong raw = BitMask.Apply(value, parameters.BitLength);

        if (parameters.Binary.BitOrder == BitOrderKind.LsbFirst)
        {
            raw = BitReverseWithinBitLength(raw, parameters.BitLength);
        }

        if (parameters.Binary.ByteOrder == ByteOrderKind.LittleEndian)
        {
            raw = ByteSwapWithinBitLength(raw, parameters.BitLength);
        }

        return BitMask.Apply(raw, parameters.BitLength);
    }

    public static ulong ByteSwapWithinBitLength(ulong value, int bitLength)
    {
        if (bitLength % 8 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitLength), bitLength, "Bit length must be divisible by 8 for byte swapping.");
        }

        int byteCount = bitLength / 8;
        ulong result = 0UL;

        for (int i = 0; i < byteCount; i++)
        {
            int sourceShift = i * 8;
            byte current = (byte)((value >> sourceShift) & 0xFFUL);
            int targetShift = (byteCount - 1 - i) * 8;
            result |= (ulong)current << targetShift;
        }

        return BitMask.Apply(result, bitLength);
    }

    public static ulong BitReverseWithinBitLength(ulong value, int bitLength)
    {
        ulong result = 0UL;
        for (int i = 0; i < bitLength; i++)
        {
            ulong bit = (value >> i) & 1UL;
            result |= bit << (bitLength - 1 - i);
        }

        return BitMask.Apply(result, bitLength);
    }
}
