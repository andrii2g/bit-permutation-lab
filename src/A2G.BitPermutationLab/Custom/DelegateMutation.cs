using A2G.BitPermutationLab.Abstractions;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Custom;

public sealed class DelegateMutation : ICustomMutation
{
    public DelegateMutation(
        string name,
        Func<ulong, CodecParameters, ulong> forwardFunc,
        Func<ulong, CodecParameters, ulong> reverseFunc)
    {
        Name = name;
        ForwardFunc = forwardFunc;
        ReverseFunc = reverseFunc;
    }

    public string Name { get; }

    public Func<ulong, CodecParameters, ulong> ForwardFunc { get; }

    public Func<ulong, CodecParameters, ulong> ReverseFunc { get; }

    public ulong Forward(ulong value, CodecParameters parameters) => ForwardFunc(value, parameters);

    public ulong Reverse(ulong value, CodecParameters parameters) => ReverseFunc(value, parameters);
}
