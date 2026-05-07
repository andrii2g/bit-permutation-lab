using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;

namespace A2G.BitPermutationLab.Dictionaries;

public static class AlphabetRegistry
{
    public const string Hex16 = "0123456789ABCDEF";
    public const string Base32Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    public const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    public static Alphabet Get(EmitterParameters emitterParameters)
    {
        return emitterParameters.Kind switch
        {
            EmitterKind.Hex16 => CreateAlphabet(AlphabetKind.Hex16, Hex16),
            EmitterKind.Base32Crockford => CreateAlphabet(AlphabetKind.Base32Crockford, Base32Crockford),
            EmitterKind.Base64Url => CreateAlphabet(AlphabetKind.Base64Url, Base64Url),
            EmitterKind.CustomAlphabet when emitterParameters.CustomAlphabet is not null =>
                CreateAlphabet(AlphabetKind.Custom, emitterParameters.CustomAlphabet),
            _ => throw new InvalidOperationException("No alphabet is defined for the requested emitter.")
        };
    }

    public static int[] CreateAsciiDecodeMap(string characters)
    {
        int[] decodeMap = Enumerable.Repeat(-1, 128).ToArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char current = characters[i];
            if (current < decodeMap.Length)
            {
                decodeMap[current] = i;
            }
        }

        return decodeMap;
    }

    private static Alphabet CreateAlphabet(AlphabetKind kind, string characters)
    {
        return new Alphabet(kind, characters, CreateAsciiDecodeMap(characters));
    }
}
