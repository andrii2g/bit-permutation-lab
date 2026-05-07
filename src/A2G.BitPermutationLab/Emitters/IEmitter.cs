using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Emitters;

public interface IEmitter
{
    EmitterKind Kind { get; }

    CodecResult Emit(ReadOnlySpan<int> chunks, CodecParameters parameters);

    int[] Read(ReadOnlySpan<char> value, CodecParameters parameters);

    int[] Read(ReadOnlySpan<byte> value, CodecParameters parameters);
}
