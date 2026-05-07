# PLAN.md - Bit Permutation Lab

## 1. Project Purpose

Create a small, focused .NET project for experimenting with very fast reversible integer encoding and decoding pipelines.

This project is **not cryptography**. It must not claim security, secrecy, or encryption. The purpose is performance research: find which reversible combinations of binary representation, mixing, permutation, chunking, and emission are fastest while still supporting deterministic reverse decoding.

The project must support:

- Built-in reversible transformation pipelines.
- Strongly specified scenario parameters.
- User-defined reversible mutation logic.
- Encode/decode validation.
- Benchmarking many parameter combinations.
- Reports as Markdown and CSV matrix tables.
- Clear skipped-scenario reporting when a parameter combination is invalid.

Main research question:

```text
Which combination of bin + mix + permute + chunk + emit logic gives the fastest reversible encode/decode performance for small, middle, and large numeric ranges?
```

Repository name:

```text
bit-permutation-lab
```

Suggested namespace:

```text
A2G.BitPermutationLab
```

---

## 2. Technology Stack

Use:

```text
.NET 10 if available
.NET 8 fallback
C# compatible with the selected target framework; the v1 contracts must compile on .NET 8/C# 12
BenchmarkDotNet
System.Numerics.BitOperations
```

The first implementation should avoid unnecessary dependencies except BenchmarkDotNet and test dependencies.

---

## 3. Repository Structure

Create this structure:

```text
bit-permutation-lab/
  README.md
  PLAN.md
  LICENSE
  .gitignore
  src/
    A2G.BitPermutationLab/
      A2G.BitPermutationLab.csproj
      Abstractions/
        ICodecPipeline.cs
        ICustomMutation.cs
        ICustomChunkMutation.cs
      Core/
        CodecPipeline.cs
        CodecParameters.cs
        CodecScenario.cs
        CodecResult.cs
        DecodeResult.cs
        FixedBinary.cs
        BitMask.cs
        SaltDerivation.cs
      Binary/
        BinaryParameters.cs
        FixedUnsignedBinary.cs
      Mixers/
        IMixer.cs
        MixerParameters.cs
        NoMixer.cs
        XorMixer.cs
        AddMixer.cs
        MultiplyMixer.cs
      Permutations/
        IPermutation.cs
        PermutationParameters.cs
        IdentityPermutation.cs
        NotPermutation.cs
        RotatePermutation.cs
        ByteSwapPermutation.cs
        BitReversePermutation.cs
        NibbleSwapPermutation.cs
        ChunkPermutation.cs
        FeistelPermutation.cs
      Chunking/
        IChunker.cs
        ChunkingParameters.cs
        FixedChunker.cs
      Emitters/
        IEmitter.cs
        EmitterParameters.cs
        HexEmitter.cs
        Base32Emitter.cs
        Base64UrlEmitter.cs
        ByteArrayEmitter.cs
      Dictionaries/
        Alphabet.cs
        AlphabetKind.cs
        AlphabetRegistry.cs
      Custom/
        DelegateMutation.cs
        DelegateChunkMutation.cs
        CustomMutationPipeline.cs
        CustomMutationRegistry.cs
      Validation/
        RoundTripValidator.cs
        ParameterValidator.cs
  tools/
    A2G.BitPermutationLab.Cli/
      A2G.BitPermutationLab.Cli.csproj
      Program.cs
      Commands/
        EncodeCommand.cs
        DecodeCommand.cs
        BenchmarkCommand.cs
        ListCommand.cs
      Configuration/
        BenchmarkConfigFile.cs
        CodecScenarioConfig.cs
        ConfigParser.cs
      Reporting/
        BenchmarkReport.cs
        BenchmarkResultRow.cs
        MarkdownReportWriter.cs
        CsvReportWriter.cs
        ConsoleTableWriter.cs
  tests/
    A2G.BitPermutationLab.Tests/
      A2G.BitPermutationLab.Tests.csproj
      RoundTripTests.cs
      ParameterValidationTests.cs
      EmitterTests.cs
      PermutationTests.cs
      MixerTests.cs
      CustomMutationTests.cs
      CliScenarioParsingTests.cs
  benchmarks/
    A2G.BitPermutationLab.Benchmarks/
      A2G.BitPermutationLab.Benchmarks.csproj
      CodecBenchmark.cs
      BenchmarkMatrixBuilder.cs
      BenchmarkValueSets.cs
  docs/
    FORMULA.md
    PARAMETERS.md
    CUSTOM_MUTATIONS.md
    FEISTEL.md
    CHUNK_PERMUTATION.md
    CLI.md
    BENCHMARKING.md
    REPORT_FORMAT.md
```

---

## 4. Core Formula

Represent every codec as a reversible pipeline:

```text
Encode(n, params) = Emit(Chunk(Permute(Mix(Bin(n), salt))))
Decode(x, params) = Int(Unmix(InversePermute(Unchunk(Read(x))), salt))
```

Compact form:

```text
x = Emit(Chunk(P(M(BinL(n), S))))
n = Int(M^-1(P^-1(Unchunk(Read(x))), S))
```

Where:

```text
n          original integer
x          encoded output
L          fixed bit length
S          salt seed
BinL       fixed-length unsigned binary representation
M          reversible mixer
P          reversible permutation
Chunk      fixed-size chunk extraction
Emit       chunk-to-output representation
Read       reverse of Emit
Unchunk    reverse of Chunk
P^-1       inverse permutation
M^-1       inverse mixer
Int        binary value to integer
```

All built-in and custom pipelines must prove:

```text
Decode(Encode(n, params), params) == n
```

This condition is required but not sufficient by itself. The implementation must also apply each inverse stage in the exact reverse pipeline order defined in this plan.

---

## 5. Numeric Scope

Initial implementation must support:

```text
uint
ulong
```

Required bit lengths:

```text
8
16
24
32
40
48
56
64
```

Rules:

```text
0 <= n < 2^BitLength
```

For bit lengths smaller than the native type size, mask unused high bits.

Example:

```text
BitLength = 24
mask = (1UL << 24) - 1
value = value & mask
```

Special case:

```text
BitLength = 64
mask = ulong.MaxValue
```

Do not implement `BigInteger` in v1.

---

## 6. Strong Scenario Model

The parameter model must describe every scenario without relying on untyped key/value fields for built-in behavior.

`ExtraParameters` or `ExtensionParameters` may exist only for third-party extension metadata. Built-in mixers, permutations, chunkers, emitters, and custom mutation placements must use typed fields.

### 6.1 Codec Scenario

Create a scenario model similar to this:

```csharp
public sealed record CodecScenario(
    string Name,
    CodecParameters Parameters,
    IReadOnlyList<ulong> Values,
    string? Notes = null);
```

### 6.2 Codec Parameters

Create `CodecParameters` with typed nested parameter objects:

```csharp
public sealed record CodecParameters(
    string Name,
    NumberKind NumberKind,
    int BitLength,
    ulong SaltSeed,
    BinaryParameters Binary,
    MixerParameters Mixer,
    PermutationParameters Permutation,
    ChunkingParameters Chunking,
    EmitterParameters Emitter,
    CustomMutationParameters? CustomMutation = null,
    CustomChunkMutationParameters? CustomChunkMutation = null);
```

Required enums:

```csharp
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
```

### 6.3 Binary Parameters

```csharp
public sealed record BinaryParameters(
    BinaryKind Kind,
    BitOrderKind BitOrder = BitOrderKind.MsbFirst,
    ByteOrderKind ByteOrder = ByteOrderKind.BigEndian);
```

For v1, `FixedUnsigned` is the only binary representation. The implementation stores the value in `ulong` internally and applies the configured bit-length mask after each reversible operation.

Binary conversion must be deterministic because it is part of the benchmarked pipeline.

`BinL(n)` behavior:

```text
raw = n & scenarioMask

if ByteOrder == LittleEndian:
    require BitLength % 8 == 0
    raw = ByteSwapWithinBitLength(raw, BitLength)

if BitOrder == LsbFirst:
    raw = BitReverseWithinBitLength(raw, BitLength)

return raw
```

`Int(v)` behavior is the exact inverse:

```text
raw = v & scenarioMask

if BitOrder == LsbFirst:
    raw = BitReverseWithinBitLength(raw, BitLength)

if ByteOrder == LittleEndian:
    require BitLength % 8 == 0
    raw = ByteSwapWithinBitLength(raw, BitLength)

return raw
```

For the first benchmark profiles, use `MsbFirst` and `BigEndian` as defaults. Non-default binary orders are still valid benchmark dimensions in the `full` profile.

### 6.4 Mixer Parameters

```csharp
public sealed record MixerParameters(
    MixerKind Kind,
    SaltDerivationKind MaskDerivation = SaltDerivationKind.SplitMix64,
    ulong? LiteralMask = null,
    ulong? LiteralAddend = null,
    ulong? Multiplier = null);
```

Interpretation:

```text
None:
  No additional fields allowed.

Xor:
  Uses LiteralMask if provided.
  Otherwise derives mask from SaltSeed using MaskDerivation.

Add:
  Uses LiteralAddend if provided.
  Otherwise derives addend from SaltSeed using MaskDerivation.

Multiply:
  Uses Multiplier if provided.
  Otherwise derives an odd multiplier from SaltSeed using MaskDerivation.
  Multiplier must be odd.
  Inverse multiplier is precomputed during pipeline construction.
```

Fields irrelevant to the selected `MixerKind` must be null. The validator must reject incompatible fields.

### 6.5 Permutation Parameters

```csharp
public sealed record PermutationParameters(
    PermutationKind Kind,
    int? RotateBy = null,
    NibbleSwapKind? NibbleSwap = null,
    int? ChunkPermutationGroupSize = null,
    ChunkPermutationVariant? ChunkPermutationVariant = null,
    IReadOnlyList<int>? ChunkPermutationOrder = null,
    int? ChunkPermutationRotateBy = null,
    int? FeistelRounds = null,
    FeistelRoundFunctionKind? FeistelRoundFunction = null);
```

Interpretation:

```text
Identity:
  No additional fields allowed.

Not:
  No additional fields allowed.

Rotate:
  Requires RotateBy.

ByteSwap:
  Requires BitLength % 8 == 0.

BitReverse:
  No additional fields allowed.

NibbleSwap:
  Requires BitLength % 4 == 0.
  Requires NibbleSwap.

ChunkPermutation:
  Requires ChunkPermutationGroupSize.
  ChunkPermutationGroupSize must be an integer from 1 to 32 inclusive and must divide BitLength.
  ChunkPermutationGroupSize is not restricted to powers of two; for example, 5 is valid for 40-bit Base32 scenarios.
  Requires ChunkPermutationVariant.
  May require ChunkPermutationOrder or ChunkPermutationRotateBy depending on variant.

Feistel:
  Requires FeistelRounds.
  Requires FeistelRoundFunction.
  Requires even BitLength.
```

Fields irrelevant to the selected `PermutationKind` must be null. The validator must reject incompatible fields.

### 6.6 Chunking Parameters

```csharp
public sealed record ChunkingParameters(
    ChunkerKind Kind,
    int ChunkSize,
    BitOrderKind ChunkReadOrder = BitOrderKind.MsbFirst);
```

`ChunkReadOrder` controls the order of chunks in the emitted output:

```text
MsbFirst:
  First emitted chunk contains the highest bits.

LsbFirst:
  First emitted chunk contains the lowest bits.
```

Both are reversible. Both should be benchmarkable.

### 6.7 Emitter Parameters

```csharp
public sealed record EmitterParameters(
    EmitterKind Kind,
    AlphabetKind AlphabetKind,
    OutputKind OutputKind,
    string? CustomAlphabet = null,
    ByteArrayTextFormat ByteArrayTextFormat = ByteArrayTextFormat.Hex);
```

Interpretation:

```text
Hex16:
  ChunkSize must be 4.
  AlphabetKind must be Hex16.
  OutputKind must be String or CharArray.

Base32Crockford:
  ChunkSize must be 5.
  AlphabetKind must be Base32Crockford.
  OutputKind must be String or CharArray.

Base64Url:
  ChunkSize must be 6.
  AlphabetKind must be Base64Url.
  OutputKind must be String or CharArray.

ByteArray:
  ChunkSize must be 8.
  AlphabetKind must be None.
  OutputKind must be ByteArray.

CustomAlphabet:
  CustomAlphabet must contain exactly 2^ChunkSize unique characters.
  OutputKind must be String or CharArray.
```

`EmitterKind`, `AlphabetKind`, and `OutputKind` are separate on purpose:

```text
EmitterKind   identifies the implementation.
AlphabetKind  identifies the dictionary used by char emitters.
OutputKind    identifies the returned representation type.
```

This resolves the matrix/reporting requirement where `Emitter` must be reported independently from `Alphabet` and `OutputKind`.

### 6.8 Custom Mutation Parameters

Bit-value custom mutations operate on the internal `ulong` value before chunking.

```csharp
public sealed record CustomMutationParameters(
    string Name,
    CustomMutationPosition Position,
    IReadOnlyDictionary<string, string> Parameters);
```

Chunk-level custom mutations operate on the chunk sequence after chunking and before emitting.

```csharp
public sealed record CustomChunkMutationParameters(
    string Name,
    CustomChunkMutationPosition Position,
    IReadOnlyDictionary<string, string> Parameters);
```

Only custom mutation parameters may use a dictionary because third-party custom algorithms are not known at compile time. Built-in scenarios must not hide built-in options in dictionaries.

For v1:

```text
At most one bit-value custom mutation per scenario.
At most one chunk-level custom mutation per scenario.
```

---

## 7. Parameter Space Summary

Required benchmarkable parameters:

```text
Parameter                         Required values / options
----------------------------------------------------------------------------------
NumberKind                        UInt32, UInt64
BitLength                         8, 16, 24, 32, 40, 48, 56, 64
Binary.Kind                       FixedUnsigned
Binary.BitOrder                   MsbFirst, LsbFirst
Binary.ByteOrder                  BigEndian, LittleEndian
SaltSeed                          ulong
Mixer.Kind                        None, Xor, Add, Multiply
Mixer.MaskDerivation              None, UseSaltSeedDirectly, SplitMix64
Mixer.LiteralMask                 optional ulong
Mixer.LiteralAddend               optional ulong
Mixer.Multiplier                  optional odd ulong
Permutation.Kind                  Identity, Not, Rotate, ByteSwap, BitReverse,
                                  NibbleSwap, ChunkPermutation, Feistel
Permutation.RotateBy              0..BitLength-1
Permutation.NibbleSwap            ReverseNibbles, SwapAdjacentNibbles
Permutation.ChunkPermutationGroupSize integer 1..32 that divides BitLength; recommended benchmark values: 1, 2, 4, 5, 6, 8, 10, 16, 32 where valid
Permutation.ChunkPermutationVariant   ExplicitOrder, ReverseGroups,
                                      RotateGroupsLeft, RotateGroupsRight,
                                      SwapAdjacentGroups, SaltShuffle
Permutation.ChunkPermutationOrder     zero-based group order, MSB-first indexing
Permutation.ChunkPermutationRotateBy  0..groupCount-1
Permutation.FeistelRounds             1, 2, 3, 4
Permutation.FeistelRoundFunction      XorShiftAdd, MultiplyXor
Chunking.Kind                     Fixed
Chunking.ChunkSize                4, 5, 6, 8
Chunking.ChunkReadOrder           MsbFirst, LsbFirst
Emitter.Kind                      Hex16, Base32Crockford, Base64Url, ByteArray,
                                  CustomAlphabet
Emitter.AlphabetKind              None, Hex16, Base32Crockford, Base64Url, Custom
Emitter.OutputKind                String, CharArray, ByteArray
Emitter.CustomAlphabet            exactly 2^ChunkSize unique chars when used
Emitter.ByteArrayTextFormat       Hex, Base64, CommaSeparatedDecimal for CLI text I/O
CustomMutation.Position           BeforeMix, AfterMix, AfterPermutation
CustomChunkMutation.Position      BeforeEmit
```

---

## 8. Built-In Mixers

All mixers operate inside `BitLength` bits and must mask output with the scenario mask.

### 8.1 NoMixer

Forward:

```text
v = n
```

Reverse:

```text
n = v
```

### 8.2 XorMixer

Forward:

```text
v = n XOR mask
```

Reverse:

```text
n = v XOR mask
```

Mask source:

```text
LiteralMask if provided, otherwise SaltDerivation(SaltSeed) & scenarioMask
```

### 8.3 AddMixer

Forward:

```text
v = (n + addend) mod 2^BitLength
```

Reverse:

```text
n = (v - addend) mod 2^BitLength
```

Always mask after operation.

### 8.4 MultiplyMixer

Forward:

```text
v = (n * multiplier) mod 2^BitLength
```

Reverse:

```text
n = (v * inverseMultiplier) mod 2^BitLength
```

Rules:

```text
multiplier must be odd
inverseMultiplier is precomputed during pipeline construction
invalid even multipliers are rejected
```

---

## 9. Salt Derivation

Salt derivation must be deterministic and cheap.

Required derivation modes:

```text
None:
  Derived value = 0.

UseSaltSeedDirectly:
  Derived value = SaltSeed.

SplitMix64:
  Derived value = SplitMix64(SaltSeed + domainConstant).
```

Use stable domain constants so mixer masks, Feistel keys, and salt-shuffle orders do not accidentally reuse the exact same stream:

```text
MixerMaskDomain      = 0x4D495845525F4D31UL
AddendDomain         = 0x4144445F4D315FUL
MultiplierDomain     = 0x4D554C5F4D315FUL
FeistelDomain        = 0x4645495354454CUL
ChunkShuffleDomain   = 0x4348554E4B5F53UL
```

### 9.1 Salt Text to SaltSeed

`CodecParameters` stores only `SaltSeed`. Any textual salt must be converted before scenario construction. The conversion is part of the public contract so CLI/config scenarios are reproducible across implementations.

Rules:

```text
--salt and --salt-text are mutually exclusive.
The string is case-sensitive.
Do not trim leading or trailing whitespace.
Unicode normalization: none.
Case folding: none.
Encoding: UTF-8 without BOM.
Terminator: no null terminator.
Hash: 64-bit FNV-1a.
Arithmetic: unchecked unsigned 64-bit overflow, equivalent to modulo 2^64.
The resulting unsigned 64-bit value is SaltSeed.
Do not use string.GetHashCode(), platform-default encodings, culture-sensitive casing, random salts, or implementation-specific hash APIs.
```

FNV-1a 64-bit contract:

```text
offsetBasis = 14695981039346656037UL
prime       = 1099511628211UL

hash = offsetBasis
for each byte b in UTF8(saltText):
    hash = hash XOR b
    hash = hash * prime modulo 2^64
SaltSeed = hash
```

C# reference implementation:

```csharp
using System.Text;

public static ulong DeriveSaltSeedFromText(string saltText)
{
    const ulong offsetBasis = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;

    ulong hash = offsetBasis;

    foreach (byte b in Encoding.UTF8.GetBytes(saltText))
    {
        hash ^= b;
        hash = unchecked(hash * prime);
    }

    return hash;
}
```

The empty string is allowed and produces the FNV offset basis value `14695981039346656037`.

Reports and exported scenario configs must include the resolved numeric `SaltSeed`. They may include the original `saltText` only as optional metadata.

Implement SplitMix64 once in `SaltDerivation` and reuse it.

Suggested implementation:

```csharp
public static ulong SplitMix64Next(ref ulong state)
{
    state += 0x9E3779B97F4A7C15UL;
    ulong z = state;
    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
    return z ^ (z >> 31);
}
```

For a `BitLength` less than 64:

```text
derivedValue &= scenarioMask
```

For `MultiplyMixer`, force odd:

```text
multiplier = (derivedValue & scenarioMask) | 1
```

---

## 10. Built-In Permutations

All permutations operate on the internal masked `ulong` bit value.

### 10.1 IdentityPermutation

Forward:

```text
v = n
```

Reverse:

```text
n = v
```

### 10.2 NotPermutation

Forward:

```text
v = (~n) & scenarioMask
```

Reverse is identical:

```text
n = (~v) & scenarioMask
```

### 10.3 RotatePermutation

Forward:

```text
v = RotateLeftWithinBitLength(n, rotateBy, BitLength)
```

Reverse:

```text
n = RotateRightWithinBitLength(v, rotateBy, BitLength)
```

Rules:

```text
rotateBy = rotateBy % BitLength
rotation must occur inside BitLength bits, not across all 64 bits
```

For non-64-bit lengths, implement with masks and shifts:

```text
left  = (value << rotateBy) & scenarioMask
right = value >> (BitLength - rotateBy)
result = (left | right) & scenarioMask
```

### 10.4 ByteSwapPermutation

Swap byte order inside `BitLength`.

Rules:

```text
BitLength % 8 == 0
Byte groups are indexed MSB-first for scenario descriptions
```

Example for 32 bits:

```text
0xAABBCCDD -> 0xDDCCBBAA
```

For 24 bits:

```text
0xAABBCC -> 0xCCBBAA
```

Reverse is identical.

### 10.5 BitReversePermutation

Reverse all bits inside `BitLength`.

Example for 8 bits:

```text
00000011 -> 11000000
```

Reverse is identical.

### 10.6 NibbleSwapPermutation

Valid when:

```text
BitLength % 4 == 0
```

Two variants:

```text
ReverseNibbles:
  reverse all 4-bit groups.

SwapAdjacentNibbles:
  [N0 N1 N2 N3] -> [N1 N0 N3 N2]
  group count must be even.
```

Reverse:

```text
ReverseNibbles is self-inverse.
SwapAdjacentNibbles is self-inverse.
```

### 10.7 ChunkPermutation

`ChunkPermutation` permutes fixed-size bit groups inside the bit value before emitter chunking.

It does **not** directly permute emitter characters unless `ChunkPermutationGroupSize == Chunking.ChunkSize`.

Rules:

```text
ChunkPermutationGroupSize must be an integer from 1 to 32 inclusive.
ChunkPermutationGroupSize must divide BitLength.
It is not restricted to powers of two.
BitLength % ChunkPermutationGroupSize == 0
groupCount = BitLength / ChunkPermutationGroupSize
Groups are indexed MSB-first in configuration and reports.
Each permutation order must contain every integer from 0 to groupCount - 1 exactly once.
```

Order contract:

```text
outputGroup[i] = inputGroup[order[i]]
```

Example for 4 groups:

```text
input groups:   [B0 B1 B2 B3]
order:          [2, 0, 3, 1]
output groups:  [B2 B0 B3 B1]
```

Inverse order is precomputed:

```text
inverseOrder[order[i]] = i
```

Supported variants:

```text
ExplicitOrder:
  Requires ChunkPermutationOrder.

ReverseGroups:
  order = [groupCount - 1, ..., 0]

RotateGroupsLeft:
  Requires ChunkPermutationRotateBy = r.
  order[i] = (i + r) % groupCount

RotateGroupsRight:
  Requires ChunkPermutationRotateBy = r.
  order[i] = (i - r + groupCount) % groupCount

SwapAdjacentGroups:
  groupCount must be even.
  order = [1, 0, 3, 2, 5, 4, ...]

SaltShuffle:
  Generate order once during pipeline construction using deterministic Fisher-Yates with SplitMix64.
```

SaltShuffle contract:

```text
state = SaltSeed + ChunkShuffleDomain + (ulong)BitLength + (ulong)ChunkPermutationGroupSize
order = [0, 1, 2, ..., groupCount - 1]
for i from groupCount - 1 down to 1:
    r = SplitMix64Next(ref state)
    j = r % (i + 1)
    swap order[i], order[j]
```

Implementation approach:

```text
Extract groups into small stackalloc buffer when groupCount <= 64.
Or use mask/shift loops with precomputed shift positions.
Precompute group masks and shift positions during pipeline construction.
Because v1 caps group size at 32, group masks can be computed safely as `(1UL << groupSize) - 1UL`.
Avoid heap allocation in hot path.
When ChunkPermutationGroupSize == BitLength, avoid `(1UL << groupSize)` and use the scenario mask directly.
```

### 10.8 FeistelPermutation

Feistel is included as a deterministic reversible permutation family for benchmarking. It must be fully specified so that two implementations produce the same result.

Rules:

```text
BitLength must be even.
halfBits = BitLength / 2
halfMask = (1UL << halfBits) - 1, except halfBits == 64 is not possible in v1.
Rounds must be 1, 2, 3, or 4.
```

Split contract:

```text
L0 = high half bits
R0 = low half bits
value = (L0 << halfBits) | R0
```

Forward rounds:

```text
L = L0
R = R0

for roundIndex in 0..rounds-1:
    K = RoundKey(roundIndex)
    F = RoundFunction(R, K, roundIndex) & halfMask
    nextL = R
    nextR = (L XOR F) & halfMask
    L = nextL
    R = nextR

result = ((L << halfBits) | R) & scenarioMask
```

Reverse rounds:

```text
L = high half bits from value
R = low half bits from value

for roundIndex in rounds-1 down to 0:
    K = RoundKey(roundIndex)
    previousR = L
    F = RoundFunction(previousR, K, roundIndex) & halfMask
    previousL = (R XOR F) & halfMask
    L = previousL
    R = previousR

result = ((L << halfBits) | R) & scenarioMask
```

Round key derivation:

```text
state = SaltSeed + FeistelDomain + (ulong)BitLength + (ulong)rounds
for i in 0..rounds-1:
    roundKey[i] = SplitMix64Next(ref state) & halfMask
```

Round function `XorShiftAdd`:

```text
y = (right ^ key) & halfMask
y = y ^ (y >> min(13, halfBits - 1))
y = (y + RotateLeftWithinBitLength(key, roundIndex + 1, halfBits)) & halfMask
return y
```

Round function `MultiplyXor`:

```text
odd = (key | 1UL) & halfMask
if odd == 0: odd = 1
y = (right * odd) & halfMask
y = y ^ RotateLeftWithinBitLength(key, roundIndex + 3, halfBits)
return y & halfMask
```

The round functions do not need to be independently reversible because Feistel structure provides reversibility.

---

## 11. Chunking

Use fixed-size chunks in v1.

Required chunk sizes:

```text
4
5
6
8
```

Rules:

```text
BitLength % ChunkSize == 0
```

If the rule is not true, reject the scenario in v1.

Do not implement padding metadata in v1.

Recommended combinations:

```text
32 bits / 4 = 8 hex chars
32 bits / 8 = 4 bytes
40 bits / 5 = 8 base32 chars
48 bits / 6 = 8 base64url chars
64 bits / 4 = 16 hex chars
64 bits / 8 = 8 bytes
```

Chunk extraction order:

```text
MsbFirst:
  for i = 0..chunkCount-1:
      shift = BitLength - ChunkSize * (i + 1)
      chunk[i] = (value >> shift) & chunkMask

LsbFirst:
  for i = 0..chunkCount-1:
      shift = ChunkSize * i
      chunk[i] = (value >> shift) & chunkMask
```

Unchunking must mirror the same order.

---

## 12. Emitters and Dictionaries

### 12.1 Hex16Emitter

```text
ChunkSize = 4
Alphabet = 0123456789ABCDEF
```

Use direct char table lookup.

### 12.2 Base32CrockfordEmitter

```text
ChunkSize = 5
Alphabet = 0123456789ABCDEFGHJKMNPQRSTVWXYZ
```

Use direct char table lookup.

### 12.3 Base64UrlEmitter

```text
ChunkSize = 6
Alphabet = ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_
```

Use direct char table lookup.

### 12.4 ByteArrayEmitter

```text
ChunkSize = 8
OutputKind = ByteArray
AlphabetKind = None
```

Emit raw bytes. This is expected to be one of the fastest emitters.

CLI text representation of byte arrays must be controlled by `ByteArrayTextFormat`:

```text
Hex:
  AABBCCDD

Base64:
  qrvM3Q==

CommaSeparatedDecimal:
  170,187,204,221
```

The internal benchmark should measure raw `byte[]` or `Span<byte>` output, not the CLI text formatting cost, unless a separate CLI-format benchmark is explicitly requested.

### 12.5 CustomAlphabetEmitter

Rules:

```text
CustomAlphabet length must equal 2^ChunkSize.
All characters must be unique.
OutputKind must be String or CharArray.
```

### 12.6 Decode Maps

Implement reverse alphabet lookup using arrays, not dictionaries.

For ASCII alphabets:

```csharp
int[] decodeMap = new int[128];
```

For custom alphabets containing non-ASCII chars, use a larger array if practical or a precomputed fallback map. Built-in alphabets must stay on fast array maps.

Initialize all values to `-1` and reject unknown characters.

Avoid `Dictionary<char, int>` in hot paths for built-ins.

---

## 13. Custom Mutation Logic

The project must support user-defined reversible mutation logic so users can benchmark custom algorithms without rewriting the whole framework.

There are two custom-mutation domains:

```text
Bit-value mutation:
  Operates on the internal masked ulong value.

Chunk-level mutation:
  Operates on the chunk sequence after chunking and before emitting.
```

### 13.1 Bit-Value Custom Mutation Interface

```csharp
public interface ICustomMutation
{
    string Name { get; }
    ulong Forward(ulong value, CodecParameters parameters);
    ulong Reverse(ulong value, CodecParameters parameters);
}
```

Rules:

```text
Forward and Reverse must be deterministic.
Reverse(Forward(v)) must return v for all validation values.
Forward must mask its result with scenarioMask.
Reverse must mask its result with scenarioMask.
The benchmark runner must reject custom mutations that fail round-trip validation.
```

### 13.2 Chunk-Level Custom Mutation Interface

Use this only for `BeforeEmit` logic.

```csharp
public interface ICustomChunkMutation
{
    string Name { get; }
    void Forward(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters);
    void Reverse(ReadOnlySpan<int> inputChunks, Span<int> outputChunks, CodecParameters parameters);
}
```

Rules:

```text
Input and output spans have the same length.
Every output chunk must be within 0..(2^ChunkSize - 1).
Reverse(Forward(chunks)) must return original chunks for validation cases.
The benchmark runner must reject invalid chunk mutations.
```

### 13.3 Delegate-Based Custom Mutations

Create adapters:

```csharp
public sealed class DelegateMutation : ICustomMutation
{
    public string Name { get; }
    public Func<ulong, CodecParameters, ulong> ForwardFunc { get; }
    public Func<ulong, CodecParameters, ulong> ReverseFunc { get; }

    public ulong Forward(ulong value, CodecParameters parameters) => ForwardFunc(value, parameters);
    public ulong Reverse(ulong value, CodecParameters parameters) => ReverseFunc(value, parameters);
}
```

Do not use the normal generic `Action` delegate family for chunk mutations with span parameters. `ReadOnlySpan<T>` and `Span<T>` are byref-like types and must not be hidden inside generic delegate type arguments. Use an explicit custom delegate or a concrete class implementing `ICustomChunkMutation`.

```csharp
public delegate void ChunkMutationDelegate(
    ReadOnlySpan<int> inputChunks,
    Span<int> outputChunks,
    CodecParameters parameters);

public sealed class DelegateChunkMutation : ICustomChunkMutation
{
    public string Name { get; }
    public ChunkMutationDelegate ForwardDelegate { get; }
    public ChunkMutationDelegate ReverseDelegate { get; }

    public DelegateChunkMutation(
        string name,
        ChunkMutationDelegate forwardDelegate,
        ChunkMutationDelegate reverseDelegate)
    {
        Name = name;
        ForwardDelegate = forwardDelegate;
        ReverseDelegate = reverseDelegate;
    }

    public void Forward(
        ReadOnlySpan<int> inputChunks,
        Span<int> outputChunks,
        CodecParameters parameters)
        => ForwardDelegate(inputChunks, outputChunks, parameters);

    public void Reverse(
        ReadOnlySpan<int> inputChunks,
        Span<int> outputChunks,
        CodecParameters parameters)
        => ReverseDelegate(inputChunks, outputChunks, parameters);
}
```

This contract is intended to compile on .NET 8/C# 12. The span values exist only as method parameters and must never be stored in fields, captured by lambdas, boxed, or placed inside generic delegate type arguments.

### 13.4 Exact Encode/Decode Placement Semantics

Base encode bit-value stages:

```text
B = BinL(n)
M = Mix(B)
P = Permute(M)
C = Chunk(P)
E = Emit(C)
```

Base decode stages:

```text
C = Read(E)
P = Unchunk(C)
M = InversePermute(P)
B = Unmix(M)
n = Int(B)
```

If a bit-value custom mutation `U` is configured, it must be applied exactly as follows.

#### BeforeMix

Encode:

```text
B = BinL(n)
B2 = U.Forward(B)
M = Mix(B2)
P = Permute(M)
C = Chunk(P)
E = Emit(C)
```

Decode:

```text
C = Read(E)
P = Unchunk(C)
M = InversePermute(P)
B2 = Unmix(M)
B = U.Reverse(B2)
n = Int(B)
```

#### AfterMix

Encode:

```text
B = BinL(n)
M = Mix(B)
M2 = U.Forward(M)
P = Permute(M2)
C = Chunk(P)
E = Emit(C)
```

Decode:

```text
C = Read(E)
P = Unchunk(C)
M2 = InversePermute(P)
M = U.Reverse(M2)
B = Unmix(M)
n = Int(B)
```

#### AfterPermutation

Encode:

```text
B = BinL(n)
M = Mix(B)
P = Permute(M)
P2 = U.Forward(P)
C = Chunk(P2)
E = Emit(C)
```

Decode:

```text
C = Read(E)
P2 = Unchunk(C)
P = U.Reverse(P2)
M = InversePermute(P)
B = Unmix(M)
n = Int(B)
```

### 13.5 Chunk Mutation BeforeEmit Semantics

If a chunk-level mutation `UC` is configured at `BeforeEmit`, it must be applied after chunking and before emitting.

Encode:

```text
B = BinL(n)
M = Mix(B)
P = Permute(M)
C = Chunk(P)
C2 = UC.Forward(C)
E = Emit(C2)
```

Decode:

```text
C2 = Read(E)
C = UC.Reverse(C2)
P = Unchunk(C)
M = InversePermute(P)
B = Unmix(M)
n = Int(B)
```

This explicit mirrored placement is required. Implementers must not guess inverse placement.

### 13.6 Custom Mutation Loading

Support two custom mutation registration paths:

```text
In-process registration:
  Used by tests and benchmarks.

Optional CLI plugin loading:
  Load an assembly path and a type name implementing ICustomMutation or ICustomChunkMutation.
```

CLI plugin loading is optional for the first internal benchmark but the config model and CLI flags must reserve the contract.

---

## 14. CLI Contract

CLI executable name:

```text
bpl
```

If the executable alias is not configured, use:

```text
A2G.BitPermutationLab.Cli
```

Commands:

```text
bpl list
bpl encode
bpl decode
bpl benchmark
```

The CLI must expose enough parameters to recreate benchmark scenarios.

### 14.1 Common Scenario Flags

These flags are shared by `encode`, `decode`, and single-scenario `benchmark` mode.

```text
--scenario-name <name>
--number-kind uint32|uint64
--bits <8|16|24|32|40|48|56|64>
--salt <ulong>
--salt-text <string>                   optional; converted to SaltSeed using Section 9.1; mutually exclusive with --salt

--bin fixed-unsigned
--bit-order msb|lsb
--byte-order big|little

--mix none|xor|add|multiply
--mask-derivation none|direct|splitmix64
--xor-mask <ulong>                     optional literal for xor
--addend <ulong>                       optional literal for add
--multiplier <odd ulong>               optional literal for multiply

--permute identity|not|rotate|byteswap|bitreverse|nibbleswap|chunk|feistel
--rotate-by <int>
--nibble-swap reverse|swap-adjacent

--chunk-permute-group-size <int>
--chunk-permute-variant explicit|reverse|rotate-left|rotate-right|swap-adjacent|salt-shuffle
--chunk-permute-order <csv>            example: 2,0,3,1
--chunk-permute-rotate-by <int>

--feistel-rounds <1|2|3|4>
--feistel-round-function xorshift-add|multiply-xor

--chunker fixed
--chunk-size <4|5|6|8>
--chunk-read-order msb|lsb

--emitter hex16|base32|base64url|bytes|custom
--alphabet none|hex16|base32-crockford|base64url|custom
--custom-alphabet <string>
--output-kind string|char-array|byte-array
--byte-array-format hex|base64|csv-decimal

--custom-mutation-name <name>
--custom-mutation-position before-mix|after-mix|after-permutation
--custom-mutation-param <key=value>     repeatable
--custom-mutation-plugin <path>
--custom-mutation-type <full.type.Name>

--custom-chunk-mutation-name <name>
--custom-chunk-mutation-position before-emit
--custom-chunk-mutation-param <key=value> repeatable
--custom-chunk-mutation-plugin <path>
--custom-chunk-mutation-type <full.type.Name>
```

Rules:

```text
Flags that do not apply to the selected kind must be rejected.
For example, --rotate-by is valid only with --permute rotate.
For example, --feistel-rounds is valid only with --permute feistel.
--salt and --salt-text are mutually exclusive.
--salt-text must use the exact derivation in section 9.1.
If neither --salt nor --salt-text is provided, use SaltSeed = 0.
```

### 14.2 list

Show available scenario component names and flag values:

```bash
bpl list
```

Output must include:

```text
Number kinds
Bit lengths
Binary kinds
Mixers
Mixer derivation modes
Permutations
Nibble swap variants
Chunk permutation variants
Feistel round functions
Chunk sizes
Chunk read orders
Emitters
Alphabets
Output kinds
Byte array text formats
Built-in benchmark profiles
Registered custom mutations
Registered custom chunk mutations
```

### 14.3 encode

Example text emitter:

```bash
bpl encode \
  --value 12345 \
  --number-kind uint32 \
  --bits 32 \
  --salt 42 \
  --bin fixed-unsigned \
  --mix xor \
  --permute rotate \
  --rotate-by 11 \
  --chunk-size 4 \
  --emitter hex16 \
  --alphabet hex16 \
  --output-kind string
```

Output:

```text
Encoded: A1B2C3D4
```

Example byte-array emitter:

```bash
bpl encode \
  --value 12345 \
  --number-kind uint64 \
  --bits 64 \
  --salt 42 \
  --mix xor \
  --permute byteswap \
  --chunk-size 8 \
  --emitter bytes \
  --alphabet none \
  --output-kind byte-array \
  --byte-array-format hex
```

Output:

```text
EncodedBytesHex: 0000000000003039
```

### 14.4 decode

Example text emitter:

```bash
bpl decode \
  --value A1B2C3D4 \
  --number-kind uint32 \
  --bits 32 \
  --salt 42 \
  --mix xor \
  --permute rotate \
  --rotate-by 11 \
  --chunk-size 4 \
  --emitter hex16 \
  --alphabet hex16 \
  --output-kind string
```

Output:

```text
Decoded: 12345
```

Example byte-array input:

```bash
bpl decode \
  --value 0000000000003039 \
  --number-kind uint64 \
  --bits 64 \
  --salt 42 \
  --mix xor \
  --permute byteswap \
  --chunk-size 8 \
  --emitter bytes \
  --alphabet none \
  --output-kind byte-array \
  --byte-array-format hex
```

### 14.5 benchmark

Profile benchmark:

```bash
bpl benchmark --profile quick
```

Config benchmark:

```bash
bpl benchmark --config benchmark.json --output reports/default.md --csv reports/default.csv
```

Single-scenario quick benchmark:

```bash
bpl benchmark \
  --mode quick \
  --iterations 1000000 \
  --value-set default \
  --number-kind uint32 \
  --bits 32 \
  --salt 42 \
  --mix xor \
  --permute rotate \
  --rotate-by 11 \
  --chunk-size 4 \
  --emitter hex16 \
  --alphabet hex16 \
  --output-kind string
```

Benchmark-specific flags:

```text
--profile quick|default|full
--config <benchmark.json>
--mode quick|benchmarkdotnet
--output <report.md>
--csv <report.csv>
--iterations <int>
--validate true|false
--value-set default|small|middle|large|max
--include-invalid true|false
--top <int>
```

---

## 15. Config File Contract

The JSON config must be able to recreate every scenario without CLI-only knowledge.

Salt in config files:

```text
A scenario may specify either parameters.saltSeed or parameters.saltText.
It must not specify both.
parameters.saltText is converted with the exact Section 9.1 FNV-1a64 rules before constructing CodecParameters.
The in-memory CodecParameters object must contain only the effective SaltSeed.
Reports must print the effective SaltSeed so a benchmark row is reproducible even when the source config used saltText.
```

### 15.1 Single Scenario Example

```json
{
  "scenarios": [
    {
      "name": "xor-rotate-hex32",
      "values": [1, 2, 1000, 10000, 1000000, 1000000000],
      "parameters": {
        "name": "xor-rotate-hex32",
        "numberKind": "UInt32",
        "bitLength": 32,
        "saltSeed": 42,
        "binary": {
          "kind": "FixedUnsigned",
          "bitOrder": "MsbFirst",
          "byteOrder": "BigEndian"
        },
        "mixer": {
          "kind": "Xor",
          "maskDerivation": "SplitMix64",
          "literalMask": null,
          "literalAddend": null,
          "multiplier": null
        },
        "permutation": {
          "kind": "Rotate",
          "rotateBy": 11,
          "nibbleSwap": null,
          "chunkPermutationGroupSize": null,
          "chunkPermutationVariant": null,
          "chunkPermutationOrder": null,
          "chunkPermutationRotateBy": null,
          "feistelRounds": null,
          "feistelRoundFunction": null
        },
        "chunking": {
          "kind": "Fixed",
          "chunkSize": 4,
          "chunkReadOrder": "MsbFirst"
        },
        "emitter": {
          "kind": "Hex16",
          "alphabetKind": "Hex16",
          "outputKind": "String",
          "customAlphabet": null,
          "byteArrayTextFormat": "Hex"
        },
        "customMutation": null,
        "customChunkMutation": null
      }
    }
  ],
  "benchmark": {
    "mode": "Quick",
    "iterations": 1000000,
    "validate": true,
    "outputMarkdown": "reports/result.md",
    "outputCsv": "reports/result.csv"
  }
}
```

### 15.2 Feistel Scenario Example

```json
{
  "name": "xor-feistel-base64url48",
  "values": [1, 2, 1000, 10000, 1000000],
  "parameters": {
    "name": "xor-feistel-base64url48",
    "numberKind": "UInt64",
    "bitLength": 48,
    "saltSeed": 987654321,
    "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
    "mixer": { "kind": "Xor", "maskDerivation": "SplitMix64" },
    "permutation": {
      "kind": "Feistel",
      "feistelRounds": 2,
      "feistelRoundFunction": "XorShiftAdd"
    },
    "chunking": { "kind": "Fixed", "chunkSize": 6, "chunkReadOrder": "MsbFirst" },
    "emitter": { "kind": "Base64Url", "alphabetKind": "Base64Url", "outputKind": "String" }
  }
}
```

### 15.3 Chunk Permutation Scenario Example

```json
{
  "name": "add-chunkperm-base32-40",
  "values": [1, 2, 1000, 10000, 1000000],
  "parameters": {
    "name": "add-chunkperm-base32-40",
    "numberKind": "UInt64",
    "bitLength": 40,
    "saltSeed": 123,
    "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
    "mixer": { "kind": "Add", "maskDerivation": "SplitMix64" },
    "permutation": {
      "kind": "ChunkPermutation",
      "chunkPermutationGroupSize": 5,
      "chunkPermutationVariant": "ExplicitOrder",
      "chunkPermutationOrder": [2, 0, 3, 1, 4, 7, 5, 6]
    },
    "chunking": { "kind": "Fixed", "chunkSize": 5, "chunkReadOrder": "MsbFirst" },
    "emitter": { "kind": "Base32Crockford", "alphabetKind": "Base32Crockford", "outputKind": "String" }
  }
}
```

### 15.4 ByteArray Scenario Example

```json
{
  "name": "xor-byteswap-bytes64",
  "values": [1, 2, 1000, 10000, 1000000, 1000000000],
  "parameters": {
    "name": "xor-byteswap-bytes64",
    "numberKind": "UInt64",
    "bitLength": 64,
    "saltSeed": 42,
    "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
    "mixer": { "kind": "Xor", "maskDerivation": "SplitMix64" },
    "permutation": { "kind": "ByteSwap" },
    "chunking": { "kind": "Fixed", "chunkSize": 8, "chunkReadOrder": "MsbFirst" },
    "emitter": {
      "kind": "ByteArray",
      "alphabetKind": "None",
      "outputKind": "ByteArray",
      "byteArrayTextFormat": "Hex"
    }
  }
}
```

### 15.5 Custom Mutation Scenario Example

```json
{
  "name": "custom-after-mix-hex32",
  "values": [1, 2, 1000, 10000],
  "parameters": {
    "name": "custom-after-mix-hex32",
    "numberKind": "UInt32",
    "bitLength": 32,
    "saltSeed": 42,
    "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
    "mixer": { "kind": "Xor", "maskDerivation": "SplitMix64" },
    "permutation": { "kind": "Rotate", "rotateBy": 7 },
    "chunking": { "kind": "Fixed", "chunkSize": 4, "chunkReadOrder": "MsbFirst" },
    "emitter": { "kind": "Hex16", "alphabetKind": "Hex16", "outputKind": "String" },
    "customMutation": {
      "name": "swap-low-high-16",
      "position": "AfterMix",
      "parameters": {
        "leftBits": "16",
        "rightBits": "16"
      }
    }
  }
}
```

---

## 16. Benchmark Value Ranges

Benchmarks must include small, middle, and large ranges.

Create value sets:

```text
SmallRange:
  1
  2

MiddleRange:
  1000
  10000

LargeRange:
  1000000
  1000000000
```

For `ulong`, optionally include max values:

```text
UInt32Max:
  4294967295

UInt64NearMax:
  18446744073709551614
```

Do not include a value that cannot fit into the configured bit length.

Example invalid pair:

```text
value = 1000000000
BitLength = 24
```

This scenario/value pair must be skipped because the value does not fit.

---

## 17. Benchmark Metrics

For each scenario/value pair, measure:

```text
Encode duration
Decode duration
Encode+Decode duration
Operations per second
Allocated bytes
Output length
Round-trip valid
```

Required modes:

```text
BenchmarkDotNet mode:
  accurate, slower, final comparison mode

Quick mode:
  stopwatch-based, less accurate, useful for exploratory parameter sweeps
```

Benchmark hot-path rules:

```text
Do not include scenario construction time.
Do not include salt parsing time.
Do not include JSON parsing time.
Do not include CLI byte-array text formatting unless explicitly benchmarking CLI formatting.
Precompute masks, derived salt values, rotate amounts, inverse multipliers, Feistel keys, chunk orders, inverse chunk orders, alphabets, and decode maps.
```

---

## 18. Benchmark Matrix

Each result row must include:

```text
ScenarioName
NumberKind
BitLength
InputRange
InputValue
SaltSeed
BinLogic
BinParameters
MixLogic
MixParameters
PermutationLogic
PermutationParameters
ChunkLogic
ChunkParameters
Emitter
Alphabet
OutputKind
OutputLength
EncodeNs
DecodeNs
RoundTripNs
OpsPerSecond
AllocatedBytes
RoundTripValid
Skipped
SkipReason
Notes
```

Markdown table example:

```markdown
| Scenario | Number | Bits | Range | Value | Bin | Mix | Permute | Chunk | Emitter | Alphabet | Out | Out Len | Encode ns | Decode ns | Total ns | Ops/sec | Alloc B | Valid |
|---|---|---:|---|---:|---|---|---|---|---|---|---|---:|---:|---:|---:|---:|---:|---|
| xor-rotate-hex32 | UInt32 | 32 | Small | 1 | fixed(msb,big) | xor(splitmix64) | rotate(11) | fixed(4,msb) | hex16 | hex16 | string | 8 | 12.1 | 13.7 | 25.8 | 38,759,689 | 0 | true |
```

CSV must contain the same columns.

---

## 19. Default Benchmark Profiles

### 19.1 quick

Use a small matrix:

```text
NumberKind:
  UInt32

BitLength:
  32

Binary:
  FixedUnsigned, MsbFirst, BigEndian

Mixer:
  None
  Xor
  Add

Permutation:
  Identity
  Rotate(rotateBy=11)
  ByteSwap

ChunkSize/Emitter:
  4/Hex16/String
  8/ByteArray/ByteArray

Values:
  1
  2
  1000
  10000
  1000000
  1000000000
```

### 19.2 default

Use:

```text
NumberKind:
  UInt32
  UInt64

BitLength:
  32
  40
  48
  64

Binary:
  FixedUnsigned, MsbFirst, BigEndian

Mixer:
  None
  Xor
  Add
  Multiply

Permutation:
  Identity
  Rotate(rotateBy=7, 11, 17)
  ByteSwap
  BitReverse
  NibbleSwap(ReverseNibbles)
  ChunkPermutation(ReverseGroups, groupSize=4/5/6/8 where valid)

ChunkSize/Emitter:
  4/Hex16/String
  5/Base32Crockford/String where valid
  6/Base64Url/String where valid
  8/ByteArray/ByteArray

Values:
  1
  2
  1000
  10000
  1000000
  1000000000
```

Skip invalid combinations.

### 19.3 full

Include:

```text
All supported bit lengths
All built-in mixers
All built-in permutations
All supported chunk emitters
BitOrder MsbFirst and LsbFirst
ByteOrder BigEndian and LittleEndian where relevant
ChunkReadOrder MsbFirst and LsbFirst
Feistel rounds 1..4
Feistel round functions XorShiftAdd and MultiplyXor
ChunkPermutation variants
Optional registered custom mutation scenarios
```

---

## 20. Scenario Validation Rules

Before running encode/decode or benchmarks, validate:

```text
General:
  BitLength is supported.
  NumberKind can contain BitLength.
  Input value fits into BitLength.
  Unknown enum values are rejected.
  Irrelevant typed option fields are null.
  CLI/config input must specify at most one of saltSeed/--salt and saltText/--salt-text.
  saltText/--salt-text is converted exactly with Section 9.1 before building CodecParameters.

Binary:
  Binary.Kind is FixedUnsigned for v1.
  Binary.ByteOrder = LittleEndian requires BitLength % 8 == 0.
  BinL and Int must apply inverse bit/byte order transforms exactly as specified in section 6.3.

Chunking:
  ChunkSize divides BitLength exactly.
  ChunkSize is supported.

Emitter:
  Hex16 requires ChunkSize = 4.
  Base32Crockford requires ChunkSize = 5.
  Base64Url requires ChunkSize = 6.
  ByteArray requires ChunkSize = 8.
  ByteArray requires AlphabetKind = None.
  ByteArray requires OutputKind = ByteArray.
  CustomAlphabet length equals 2^ChunkSize.
  CustomAlphabet characters are unique.

Mixer:
  None has no mixer-specific fields.
  Xor does not accept LiteralAddend or Multiplier.
  Add does not accept LiteralMask or Multiplier.
  Multiply does not accept LiteralMask or LiteralAddend.
  Multiply multiplier is odd.

Permutation:
  Identity has no permutation-specific fields.
  Not has no permutation-specific fields.
  Rotate requires RotateBy.
  RotateBy is normalized with modulo BitLength.
  ByteSwap requires BitLength % 8 == 0.
  NibbleSwap requires BitLength % 4 == 0.
  SwapAdjacentNibbles requires an even nibble count.
  ChunkPermutation requires valid group size and variant.
  ChunkPermutationGroupSize is 1..32 inclusive and must divide BitLength; powers of two are not required.
  ChunkPermutation explicit order is a complete permutation.
  ChunkPermutation rotate variants require rotate amount.
  ChunkPermutation swap-adjacent requires even group count.
  Feistel requires even BitLength.
  Feistel rounds must be 1..4.
  Feistel requires a round function.

Custom:
  Custom mutation name must exist in registry or plugin must load.
  Custom mutation position must match mutation domain.
  Bit-value custom mutation passes round-trip validation.
  Chunk-level custom mutation uses ICustomChunkMutation or ChunkMutationDelegate, not the generic Action delegate family with Span generic arguments.
  Chunk-level custom mutation passes round-trip validation.
```

Invalid combinations must be skipped for matrix benchmark mode and must be shown in the report summary.

For direct `encode` and `decode`, invalid combinations should return a non-zero exit code and a clear error message.

---

## 21. Report Summary

The Markdown report must contain:

```text
# Bit Permutation Lab Benchmark Report

## Environment
- OS
- CPU
- Runtime
- Build configuration
- Timestamp

## Parameters
- Profile
- Mode
- Iterations
- Value ranges
- Scenario count
- Skipped scenario count

## Top Results
- Fastest encode
- Fastest decode
- Fastest round-trip
- Lowest allocation
- Shortest output

## Matrix Results
Main results table

## Skipped Scenarios
Scenario/component combinations and reasons

## Notes
Warnings or observations
```

---

## 22. Performance Guidelines

Implementation must optimize for speed:

```text
Avoid allocations in encode/decode hot paths.
Use Span<char>, Span<byte>, and stackalloc where possible.
Use precomputed alphabets and decode maps.
Avoid StringBuilder in hot paths.
Avoid Dictionary<char, int> in built-in decode hot paths.
Avoid LINQ in hot paths.
Avoid reflection in hot paths.
Precompute masks, rotate amounts, multipliers, inverse multipliers, Feistel keys, lookup tables, chunk orders, and inverse orders during pipeline construction.
Use Release configuration for benchmarks.
Separate CLI formatting costs from core codec costs.
```

Expected likely-fast candidates:

```text
ByteArray emitter with ChunkSize=8
Hex16 emitter with direct lookup
Xor + Rotate
Add + Rotate
ByteSwap + Xor
Identity + ByteArray baseline
```

Expected slower candidates:

```text
Feistel with multiple rounds
Custom plugin mutations using reflection in hot path
Custom alphabets with non-ASCII decode maps
Decimal/string-heavy emitters
```

---

## 23. Tests

Add tests for:

```text
Round-trip correctness for each built-in mixer.
Round-trip correctness for each built-in permutation.
Round-trip correctness for each emitter.
Round-trip correctness for MsbFirst and LsbFirst chunk order.
Round-trip correctness for ChunkPermutation explicit order.
Round-trip correctness for ChunkPermutation SaltShuffle determinism.
Round-trip correctness for Feistel for all supported even bit lengths.
Invalid parameter rejection.
Input value too large for bit length.
Invalid character decoding.
Custom bit-value mutation success.
Custom bit-value mutation failure detection.
Custom chunk mutation success.
Custom chunk mutation failure detection.
CLI scenario parsing for representative scenarios.
Config JSON parsing for representative scenarios.
```

Minimum round-trip test values:

```text
0
1
2
3
15
16
31
32
63
64
127
128
255
256
1000
10000
1000000
uint.MaxValue where applicable
ulong.MaxValue where applicable
```

For each `BitLength`, also test:

```text
0
1
maxValueForBitLength - 1
maxValueForBitLength
```

---

## 24. README Requirements

README.md must explain:

```text
What the project does.
That it is not cryptography.
The reversible formula.
The scenario model.
How to run encode/decode.
How to run quick/default/full benchmarks.
How to read the report.
How to add a custom mutation.
How to use config JSON.
```

Keep the root README concise and move detailed explanations into docs.

---

## 25. Initial Implementation Order

Implement in this order:

```text
1. Create solution and projects.
2. Add core enums and typed parameter models.
3. Add parameter validation.
4. Implement mask helpers and fixed binary helpers.
5. Implement salt derivation.
6. Implement mixers.
7. Implement simple permutations: identity, not, rotate, byteswap, bitreverse, nibbleswap.
8. Implement deterministic ChunkPermutation.
9. Implement deterministic FeistelPermutation.
10. Implement chunker.
11. Implement emitters.
12. Implement pipeline encode/decode without custom mutations.
13. Add round-trip validator.
14. Add tests for built-ins.
15. Add custom bit-value mutation support.
16. Add custom chunk mutation support.
17. Add tests for custom mutations and inverse placement.
18. Add CLI encode/decode/list commands.
19. Add config JSON parsing.
20. Add quick benchmark mode.
21. Add BenchmarkDotNet mode.
22. Add Markdown and CSV report writers.
23. Add docs.
24. Finalize README.
```

---

## 26. Acceptance Criteria

The work is complete when:

```text
The solution builds successfully.
Tests pass.
bpl list prints all supported component names and parameter values.
bpl encode can encode a number using selected parameters.
bpl decode can decode it back.
ByteArray emitter works through CLI using configured byte-array text format.
Built-in scenarios pass round-trip validation.
Custom bit-value mutation can be registered and validated.
Custom chunk mutation can be registered and validated.
Custom mutation inverse placement is tested for BeforeMix, AfterMix, AfterPermutation, and BeforeEmit chunk mutation.
Feistel results are deterministic and round-trip valid.
ChunkPermutation results are deterministic and round-trip valid.
bpl benchmark --profile quick produces console output.
bpl benchmark --profile default --output report.md --csv report.csv creates Markdown and CSV reports.
The Markdown report contains a matrix with bin, mix, permute, chunk, emit logic names, typed parameters, duration, allocation, output length, and validity.
Invalid combinations are skipped with clear reasons in benchmark mode.
Invalid direct encode/decode requests return a non-zero CLI exit code.
Hot paths avoid unnecessary allocations.
```

---

## 27. Out of Scope for v1

Do not implement in v1:

```text
Real encryption.
Elliptic curves.
Public/private keys.
Cryptographic security claims.
BigInteger support.
Network service mode.
GUI.
Database persistence.
Distributed benchmarks.
Automatic algorithm search using AI.
Runtime execution of arbitrary source-code strings from the CLI.
```

Plugin assemblies may be supported, but the CLI must not eval source text.

---

## 28. Important Design Note

This project is a performance lab for reversible transformations.

Use wording like:

```text
reversible encoding
fast reversible transform
salted reversible mapping
benchmark codec
bit permutation lab
```

Avoid wording like:

```text
encryption
secure cipher
secret protection
cryptographic hash
```

---

## 29. Review Issues Fixed In This Plan

This version intentionally fixes these specification gaps:

```text
1. CodecParameters now has typed nested parameter models, including EmitterParameters and EmitterKind.
2. Mixer-specific parameters are typed instead of buried in ExtraParameters.
3. Permutation-specific parameters are typed: rotateBy, Feistel rounds/function, chunk permutation group size/order/variant, nibble mode.
4. Custom mutation inverse placement is explicitly defined for every supported position.
5. BeforeEmit is moved to a chunk-level mutation interface because bit-value mutation before emit was ambiguous.
6. Feistel split strategy, round keys, round functions, and reverse algorithm are deterministic.
7. ChunkPermutation now specifies group domain, order encoding, indexing, inverse order, and salt-shuffle derivation.
8. CLI flags now map to all scenario parameters needed to recreate benchmark scenarios.
9. JSON config examples cover normal, Feistel, chunk permutation, byte-array, and custom mutation scenarios.
10. DelegateChunkMutation avoids invalid generic delegate type arguments for Span/ReadOnlySpan by using explicit methods and a custom non-generic delegate type.
11. ChunkPermutationGroupSize consistently supports any integer divisor of BitLength, including 5-bit and 6-bit groups.
12. --salt-text is reproducible because normalization, UTF-8 encoding, and 64-bit FNV-1a conversion are specified.
```
