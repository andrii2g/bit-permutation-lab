using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Abstractions;

public interface ICodecPipeline
{
    CodecResult Encode(ulong value, CodecParameters parameters);

    DecodeResult Decode(ReadOnlySpan<char> value, CodecParameters parameters);

    DecodeResult Decode(ReadOnlySpan<byte> value, CodecParameters parameters);
}
