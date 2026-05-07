namespace A2G.BitPermutationLab.Core;

public sealed record CodecResult(
    OutputKind OutputKind,
    int OutputLength,
    string? StringValue = null,
    char[]? CharArrayValue = null,
    byte[]? ByteArrayValue = null);
