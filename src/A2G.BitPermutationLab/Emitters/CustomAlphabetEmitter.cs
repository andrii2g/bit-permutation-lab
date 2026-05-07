using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Dictionaries;

namespace A2G.BitPermutationLab.Emitters;

public sealed class CustomAlphabetEmitter : IEmitter
{
    public EmitterKind Kind => EmitterKind.CustomAlphabet;

    public CodecResult Emit(ReadOnlySpan<int> chunks, CodecParameters parameters) =>
        AlphabetEmitterSupport.EmitChars(chunks, parameters, AlphabetRegistry.Get(parameters.Emitter));

    public int[] Read(ReadOnlySpan<char> value, CodecParameters parameters) =>
        AlphabetEmitterSupport.ReadChars(value, AlphabetRegistry.Get(parameters.Emitter));

    public int[] Read(ReadOnlySpan<byte> value, CodecParameters parameters) =>
        throw new InvalidOperationException("Custom alphabet emitter reads char input, not byte input.");
}
