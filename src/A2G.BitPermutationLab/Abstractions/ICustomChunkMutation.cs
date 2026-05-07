using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Abstractions;

public interface ICustomChunkMutation
{
    string Name { get; }

    void Forward(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters);

    void Reverse(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters);
}
