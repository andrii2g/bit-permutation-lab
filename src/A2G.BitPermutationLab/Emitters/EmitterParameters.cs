using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Emitters;

public sealed record EmitterParameters(
    EmitterKind Kind,
    AlphabetKind AlphabetKind,
    OutputKind OutputKind,
    string? CustomAlphabet = null,
    ByteArrayTextFormat ByteArrayTextFormat = ByteArrayTextFormat.Hex);
