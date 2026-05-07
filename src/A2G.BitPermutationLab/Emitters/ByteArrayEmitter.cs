using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Emitters;

public sealed class ByteArrayEmitter : IEmitter
{
    public EmitterKind Kind => EmitterKind.ByteArray;

    public CodecResult Emit(ReadOnlySpan<int> chunks, CodecParameters parameters)
    {
        byte[] bytes = new byte[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
        {
            bytes[i] = (byte)chunks[i];
        }

        return new CodecResult(OutputKind.ByteArray, bytes.Length, ByteArrayValue: bytes);
    }

    public int[] Read(ReadOnlySpan<char> value, CodecParameters parameters) =>
        throw new InvalidOperationException("ByteArray emitter reads byte input, not char input.");

    public int[] Read(ReadOnlySpan<byte> value, CodecParameters parameters)
    {
        int[] chunks = new int[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            chunks[i] = value[i];
        }

        return chunks;
    }
}
