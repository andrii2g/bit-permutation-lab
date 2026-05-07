using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Dictionaries;

namespace A2G.BitPermutationLab.Emitters;

internal static class AlphabetEmitterSupport
{
    public static CodecResult EmitChars(ReadOnlySpan<int> chunks, CodecParameters parameters, Alphabet alphabet)
    {
        char[] chars = new char[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
        {
            chars[i] = alphabet.Characters[chunks[i]];
        }

        return parameters.Emitter.OutputKind switch
        {
            OutputKind.String => new CodecResult(OutputKind.String, chars.Length, StringValue: new string(chars)),
            OutputKind.CharArray => new CodecResult(OutputKind.CharArray, chars.Length, CharArrayValue: chars),
            _ => throw new InvalidOperationException("Char-based emitters support only String or CharArray output kinds.")
        };
    }

    public static int[] ReadChars(ReadOnlySpan<char> value, Alphabet alphabet)
    {
        int[] chunks = new int[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (current >= alphabet.DecodeMap.Length || alphabet.DecodeMap[current] < 0)
            {
                throw new InvalidOperationException($"Character '{current}' is not valid for alphabet '{alphabet.Kind}'.");
            }

            chunks[i] = alphabet.DecodeMap[current];
        }

        return chunks;
    }
}
