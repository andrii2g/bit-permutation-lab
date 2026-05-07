using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Abstractions;

public interface ICustomMutation
{
    string Name { get; }

    ulong Forward(ulong value, CodecParameters parameters);

    ulong Reverse(ulong value, CodecParameters parameters);
}
