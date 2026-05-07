using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;
using A2G.BitPermutationLab.Validation;

namespace A2G.BitPermutationLab.Tests;

public sealed class ParameterValidatorTests
{
    [Fact]
    public void Rejects_Value_ThatDoesNotFitBitLength()
    {
        CodecParameters parameters = CreateParameters(bitLength: 24, numberKind: NumberKind.UInt32);

        ValidationResult result = ParameterValidator.ValidateValue(1000000000UL, parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "Value");
    }

    [Fact]
    public void Rejects_UInt32_WithBitLengthAbove32()
    {
        CodecParameters parameters = CreateParameters(bitLength: 40, numberKind: NumberKind.UInt32);

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "NumberKind");
    }

    [Fact]
    public void Rejects_UnsupportedChunkSize()
    {
        CodecParameters parameters = CreateParameters(bitLength: 32, numberKind: NumberKind.UInt32, chunkSize: 7);

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "ChunkSize");
    }

    [Fact]
    public void Rejects_EvenLiteralMultiplier()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 32,
            numberKind: NumberKind.UInt32,
            mixerParameters: new MixerParameters(MixerKind.Multiply, Multiplier: 10));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "Multiplier");
    }

    [Fact]
    public void Rejects_ByteArrayEmitter_WithNonByteOutput()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 64,
            numberKind: NumberKind.UInt64,
            chunkSize: 8,
            emitterParameters: new EmitterParameters(EmitterKind.ByteArray, AlphabetKind.None, OutputKind.String));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "Emitter");
    }

    [Fact]
    public void Rejects_HexEmitter_WithWrongChunkSize()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 32,
            numberKind: NumberKind.UInt32,
            chunkSize: 5);

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Message.Contains("Hex16", StringComparison.Ordinal));
    }

    [Fact]
    public void Accepts_ByteSwap_WhenBitLengthIsByteAligned()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 24,
            numberKind: NumberKind.UInt32,
            permutationParameters: new PermutationParameters(PermutationKind.ByteSwap),
            chunkSize: 6,
            emitterParameters: new EmitterParameters(EmitterKind.Base64Url, AlphabetKind.Base64Url, OutputKind.String));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.True(result.IsValid);

        parameters = parameters with
        {
            NumberKind = NumberKind.UInt64,
            BitLength = 40,
            Chunking = new ChunkingParameters(ChunkerKind.Fixed, 5),
            Emitter = new EmitterParameters(EmitterKind.Base32Crockford, AlphabetKind.Base32Crockford, OutputKind.String)
        };
        result = ParameterValidator.Validate(parameters with { Permutation = new PermutationParameters(PermutationKind.ByteSwap) });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_Rotate_WhenRotateByMissing()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 32,
            numberKind: NumberKind.UInt32,
            permutationParameters: new PermutationParameters(PermutationKind.Rotate));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "RotateBy");
    }

    [Fact]
    public void Rejects_Feistel_WithoutRoundsOrRoundFunction()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 32,
            numberKind: NumberKind.UInt32,
            permutationParameters: new PermutationParameters(PermutationKind.Feistel));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "FeistelRounds");
        Assert.Contains(result.Errors, error => error.Code == "FeistelRoundFunction");
    }

    [Fact]
    public void Rejects_ChunkPermutation_WithInvalidExplicitOrder()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 32,
            numberKind: NumberKind.UInt32,
            permutationParameters: new PermutationParameters(
                PermutationKind.ChunkPermutation,
                ChunkPermutationGroupSize: 4,
                ChunkPermutationVariant: ChunkPermutationVariant.ExplicitOrder,
                ChunkPermutationOrder: [0, 1, 2]));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "ChunkPermutationOrder");
    }

    [Fact]
    public void Accepts_ImplementedSimpleScenario()
    {
        CodecParameters parameters = CreateParameters(
            bitLength: 32,
            numberKind: NumberKind.UInt32,
            permutationParameters: new PermutationParameters(PermutationKind.Rotate, RotateBy: 11),
            mixerParameters: new MixerParameters(MixerKind.Xor));

        ValidationResult result = ParameterValidator.Validate(parameters);

        Assert.True(result.IsValid);
    }

    private static CodecParameters CreateParameters(
        int bitLength,
        NumberKind numberKind,
        int chunkSize = 4,
        MixerParameters? mixerParameters = null,
        PermutationParameters? permutationParameters = null,
        EmitterParameters? emitterParameters = null)
    {
        return new CodecParameters(
            "validation-test",
            numberKind,
            bitLength,
            0UL,
            new BinaryParameters(BinaryKind.FixedUnsigned),
            mixerParameters ?? new MixerParameters(MixerKind.None),
            permutationParameters ?? new PermutationParameters(PermutationKind.Identity),
            new ChunkingParameters(ChunkerKind.Fixed, chunkSize),
            emitterParameters ?? new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String));
    }
}
