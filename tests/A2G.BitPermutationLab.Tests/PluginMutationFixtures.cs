using A2G.BitPermutationLab.Abstractions;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Tests;

public sealed class PluginXorMutation : ICustomMutation
{
    public string Name => "plugin-xor";

    public ulong Forward(ulong value, CodecParameters parameters) => value ^ 0x0000FFFFUL;

    public ulong Reverse(ulong value, CodecParameters parameters) => value ^ 0x0000FFFFUL;
}

public sealed class PluginReverseChunkMutation : ICustomChunkMutation
{
    public string Name => "plugin-reverse-chunks";

    public void Forward(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters)
    {
        for (int index = 0; index < inputChunks.Length; index++)
        {
            outputChunks[index] = inputChunks[inputChunks.Length - 1 - index];
        }
    }

    public void Reverse(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters)
    {
        for (int index = 0; index < inputChunks.Length; index++)
        {
            outputChunks[index] = inputChunks[inputChunks.Length - 1 - index];
        }
    }
}
