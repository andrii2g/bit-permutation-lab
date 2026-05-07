namespace A2G.BitPermutationLab.Core;

public sealed record DecodeResult(
    ulong Value,
    bool Success,
    string? ErrorMessage = null);
