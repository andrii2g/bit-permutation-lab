namespace A2G.BitPermutationLab.Core;

public sealed record CodecParameters(
    string Name,
    NumberKind NumberKind,
    int BitLength,
    ulong SaltSeed,
    Binary.BinaryParameters Binary,
    Mixers.MixerParameters Mixer,
    Permutations.PermutationParameters Permutation,
    Chunking.ChunkingParameters Chunking,
    Emitters.EmitterParameters Emitter,
    Custom.CustomMutationParameters? CustomMutation = null,
    Custom.CustomChunkMutationParameters? CustomChunkMutation = null);

public enum NumberKind
{
    UInt32,
    UInt64
}

public enum BinaryKind
{
    FixedUnsigned
}

public enum BitOrderKind
{
    MsbFirst,
    LsbFirst
}

public enum ByteOrderKind
{
    BigEndian,
    LittleEndian
}

public enum MixerKind
{
    None,
    Xor,
    Add,
    Multiply
}

public enum SaltDerivationKind
{
    None,
    UseSaltSeedDirectly,
    SplitMix64
}

public enum PermutationKind
{
    Identity,
    Not,
    Rotate,
    ByteSwap,
    BitReverse,
    NibbleSwap,
    ChunkPermutation,
    Feistel
}

public enum NibbleSwapKind
{
    ReverseNibbles,
    SwapAdjacentNibbles
}

public enum ChunkPermutationVariant
{
    ExplicitOrder,
    ReverseGroups,
    RotateGroupsLeft,
    RotateGroupsRight,
    SwapAdjacentGroups,
    SaltShuffle
}

public enum FeistelRoundFunctionKind
{
    XorShiftAdd,
    MultiplyXor
}

public enum ChunkerKind
{
    Fixed
}

public enum EmitterKind
{
    Hex16,
    Base32Crockford,
    Base64Url,
    ByteArray,
    CustomAlphabet
}

public enum AlphabetKind
{
    None,
    Hex16,
    Base32Crockford,
    Base64Url,
    Custom
}

public enum OutputKind
{
    String,
    CharArray,
    ByteArray
}

public enum ByteArrayTextFormat
{
    Hex,
    Base64,
    CommaSeparatedDecimal
}

public enum CustomMutationPosition
{
    BeforeMix,
    AfterMix,
    AfterPermutation
}

public enum CustomChunkMutationPosition
{
    BeforeEmit
}
