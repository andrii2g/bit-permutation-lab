using A2G.BitPermutationLab.Abstractions;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Custom;

public delegate void ChunkMutationDelegate(
    ReadOnlySpan<int> inputChunks,
    Span<int> outputChunks,
    CodecParameters parameters);

public sealed class DelegateChunkMutation : ICustomChunkMutation
{
    public DelegateChunkMutation(
        string name,
        ChunkMutationDelegate forwardDelegate,
        ChunkMutationDelegate reverseDelegate)
    {
        Name = name;
        ForwardDelegate = forwardDelegate;
        ReverseDelegate = reverseDelegate;
    }

    public string Name { get; }

    public ChunkMutationDelegate ForwardDelegate { get; }

    public ChunkMutationDelegate ReverseDelegate { get; }

    public void Forward(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters) =>
        ForwardDelegate(inputChunks, outputChunks, parameters);

    public void Reverse(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters) =>
        ReverseDelegate(inputChunks, outputChunks, parameters);
}
