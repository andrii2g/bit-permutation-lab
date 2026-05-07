using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Emitters;

public static class EmitterFactory
{
    public static IEmitter Create(EmitterKind kind)
    {
        return kind switch
        {
            EmitterKind.Hex16 => new HexEmitter(),
            EmitterKind.Base32Crockford => new Base32Emitter(),
            EmitterKind.Base64Url => new Base64UrlEmitter(),
            EmitterKind.ByteArray => new ByteArrayEmitter(),
            EmitterKind.CustomAlphabet => new CustomAlphabetEmitter(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported emitter kind.")
        };
    }
}
