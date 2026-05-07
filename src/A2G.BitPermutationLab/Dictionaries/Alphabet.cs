using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Dictionaries;

public sealed record Alphabet(
    AlphabetKind Kind,
    string Characters,
    int[] DecodeMap);
