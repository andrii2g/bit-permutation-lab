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

    private static CodecParameters CreateParameters(
        int bitLength,
        NumberKind numberKind,
        int chunkSize = 4,
        MixerParameters? mixerParameters = null)
    {
        return new CodecParameters(
            "validation-test",
            numberKind,
            bitLength,
            0UL,
            new BinaryParameters(BinaryKind.FixedUnsigned),
            mixerParameters ?? new MixerParameters(MixerKind.None),
            new PermutationParameters(PermutationKind.Identity),
            new ChunkingParameters(ChunkerKind.Fixed, chunkSize),
            new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String));
    }
}
